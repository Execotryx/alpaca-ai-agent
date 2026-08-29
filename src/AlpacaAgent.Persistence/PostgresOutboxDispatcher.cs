using AlpacaAgent.Application.Ports;
using AlpacaAgent.Domain.Workflow;
using Npgsql;

namespace AlpacaAgent.Persistence;

public sealed class PostgresOutboxDispatcher(NpgsqlDataSource dataSource) : IDurableOutboxDispatcher
{
    public async Task<DurableOutboxClaim?> ClaimAsync(string owner, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
WITH candidate AS (
 SELECT o.outbox_id FROM outbox_messages o LEFT JOIN workflow_cycles c ON c.cycle_id=o.cycle_id
 WHERE o.status='PENDING' AND o.due_at<=clock_timestamp()
 AND o.attempt_count<o.maximum_attempts AND o.message_type IN ('ALERT','RECORDER','TEST')
 AND NOT EXISTS(SELECT 1 FROM control_states x WHERE x.active AND (x.scope_type IN ('GLOBAL_PAUSE','KILL_SWITCH') OR (x.scope_type='SYMBOL_PAUSE' AND x.scope_key=c.symbol) OR (x.scope_type='STRATEGY_PAUSE' AND x.scope_key=c.strategy)))
 ORDER BY o.due_at,o.outbox_id FOR UPDATE OF o SKIP LOCKED LIMIT 1)
UPDATE outbox_messages o SET status='DISPATCHING',lease_owner=$1,lease_expires_at=clock_timestamp()+$2::interval,
 fencing_token=o.fencing_token+1,attempt_count=o.attempt_count+1
FROM candidate c WHERE o.outbox_id=c.outbox_id
RETURNING o.outbox_id,o.deduplication_key,o.message_type,o.message_version,o.payload::text,o.fencing_token,o.lease_expires_at,o.attempt_count,o.maximum_attempts,clock_timestamp()
""", connection, transaction);
        command.Parameters.AddWithValue(owner); command.Parameters.AddWithValue(leaseDuration);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) { await reader.DisposeAsync(); await transaction.CommitAsync(cancellationToken); return null; }
        var outboxId=reader.GetGuid(0); var dedupe=reader.GetString(1); var type=reader.GetString(2); var version=reader.GetString(3); var payload=reader.GetString(4);
        var fence=reader.GetInt64(5); var expiry=ReadUtc(reader,6); var attemptNumber=reader.GetInt32(7); var maximum=reader.GetInt32(8); var now=ReadUtc(reader,9);
        await reader.DisposeAsync(); var attemptId=Guid.NewGuid();
        await using var attempt = new NpgsqlCommand("INSERT INTO outbox_dispatch_attempts(attempt_id,outbox_id,attempt_number,owner,fencing_token,database_claim_time) VALUES($1,$2,$3,$4,$5,$6)",connection,transaction);
        attempt.Parameters.AddWithValue(attemptId); attempt.Parameters.AddWithValue(outboxId); attempt.Parameters.AddWithValue(attemptNumber); attempt.Parameters.AddWithValue(owner); attempt.Parameters.AddWithValue(fence); attempt.Parameters.AddWithValue(now);
        await attempt.ExecuteNonQueryAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return new(attemptId,outboxId,dedupe,type,version,payload,owner,fence,expiry,attemptNumber,maximum);
    }

    public async Task<OutboxDispatchResult> DispatchAsync(DurableOutboxClaim claim, IPhase3MessageSink sink, CancellationToken cancellationToken)
    {
        var preAuthorized = await HasDispatchAuthorityAsync(claim, cancellationToken);
        Exception? failure = null;
        try { if (preAuthorized) await sink.DeliverAsync(claim.DeduplicationKey,claim.MessageType,claim.MessageVersion,claim.PayloadJson,cancellationToken); }
        catch (Exception exception) when (exception is not OperationCanceledException) { failure=exception; }
        await using var connection=await dataSource.OpenConnectionAsync(cancellationToken); await using var transaction=await connection.BeginTransactionAsync(cancellationToken);
        string status; string? owner; long fence; DateTimeOffset? lease; DateTimeOffset now;
        await using (var current=new NpgsqlCommand("SELECT status,lease_owner,fencing_token,lease_expires_at,clock_timestamp() FROM outbox_messages WHERE outbox_id=$1 FOR UPDATE",connection,transaction))
        { current.Parameters.AddWithValue(claim.OutboxId); await using var reader=await current.ExecuteReaderAsync(cancellationToken); if(!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Outbox message no longer exists."); status=reader.GetString(0);owner=reader.IsDBNull(1)?null:reader.GetString(1);fence=reader.GetInt64(2);lease=reader.IsDBNull(3)?null:ReadUtc(reader,3);now=ReadUtc(reader,4); }
        var controlActive = await ControlActiveAsync(connection, transaction, claim.OutboxId, cancellationToken);
        var authority=preAuthorized&&!controlActive&&status=="DISPATCHING"&&owner==claim.Owner&&fence==claim.FencingToken&&lease>now;
        var exhausted=claim.AttemptNumber>=claim.MaximumAttempts; var classification=!authority?"LOST_AUTHORITY":failure is null?"DELIVERED":exhausted?"DEAD_LETTER":"DELIVERY_FAILED";
        await using (var result=new NpgsqlCommand("INSERT INTO outbox_dispatch_attempt_results(result_id,attempt_id,observed_at,authoritative,outcome,classification) VALUES($1,$2,$3,$4,$5,$6)",connection,transaction))
        { result.Parameters.AddWithValue(Guid.NewGuid());result.Parameters.AddWithValue(claim.AttemptId);result.Parameters.AddWithValue(now);result.Parameters.AddWithValue(authority);result.Parameters.AddWithValue(failure is null?"COMPLETED":"FAILED");result.Parameters.AddWithValue(classification);await result.ExecuteNonQueryAsync(cancellationToken); }
        if(!authority){await transaction.CommitAsync(cancellationToken);return new(false,false,classification);}
        await using (var update=new NpgsqlCommand("UPDATE outbox_messages SET status=$2,due_at=CASE WHEN $2='PENDING' THEN clock_timestamp()+interval '5 seconds' ELSE due_at END,lease_owner=NULL,lease_expires_at=NULL WHERE outbox_id=$1",connection,transaction))
        { update.Parameters.AddWithValue(claim.OutboxId);update.Parameters.AddWithValue(failure is null?"COMPLETED":exhausted?"DEAD_LETTER":"PENDING");await update.ExecuteNonQueryAsync(cancellationToken); }
        await transaction.CommitAsync(cancellationToken); return new(failure is null,failure is not null&&!exhausted,classification);
    }

    public async Task<int> RecoverExpiredAsync(CancellationToken cancellationToken)
    {
        await using var connection=await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction=await connection.BeginTransactionAsync(cancellationToken);
        var expired = new List<(Guid OutboxId, Guid AttemptId, bool Retryable)>();
        await using (var select=new NpgsqlCommand("""
SELECT o.outbox_id,a.attempt_id,o.attempt_count<o.maximum_attempts
FROM outbox_messages o
JOIN outbox_dispatch_attempts a ON a.outbox_id=o.outbox_id AND a.attempt_number=o.attempt_count
WHERE o.status='DISPATCHING' AND o.lease_expires_at<=clock_timestamp()
FOR UPDATE OF o SKIP LOCKED
""",connection,transaction))
        await using (var reader=await select.ExecuteReaderAsync(cancellationToken))
            while(await reader.ReadAsync(cancellationToken)) expired.Add((reader.GetGuid(0),reader.GetGuid(1),reader.GetBoolean(2)));
        await using var nowCommand=new NpgsqlCommand("SELECT clock_timestamp()",connection,transaction);
        var now=ToUtc(await nowCommand.ExecuteScalarAsync(cancellationToken));
        foreach(var item in expired)
        {
            await using (var result=new NpgsqlCommand("INSERT INTO outbox_dispatch_attempt_results(result_id,attempt_id,observed_at,authoritative,outcome,classification) VALUES($1,$2,$3,true,'LOST_LEASE','LEASE_EXPIRED') ON CONFLICT (attempt_id) WHERE authoritative DO NOTHING",connection,transaction))
            { result.Parameters.AddWithValue(Guid.NewGuid());result.Parameters.AddWithValue(item.AttemptId);result.Parameters.AddWithValue(now);await result.ExecuteNonQueryAsync(cancellationToken); }
            await using var update=new NpgsqlCommand("UPDATE outbox_messages SET status=$2,due_at=$3,lease_owner=NULL,lease_expires_at=NULL WHERE outbox_id=$1",connection,transaction);
            update.Parameters.AddWithValue(item.OutboxId);update.Parameters.AddWithValue(item.Retryable?"PENDING":"DEAD_LETTER");update.Parameters.AddWithValue(now);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return expired.Count;
    }

    private async Task<bool> HasDispatchAuthorityAsync(DurableOutboxClaim claim, CancellationToken cancellationToken)
    {
        await using var command=dataSource.CreateCommand("SELECT EXISTS(SELECT 1 FROM outbox_messages o LEFT JOIN workflow_cycles c ON c.cycle_id=o.cycle_id WHERE o.outbox_id=$1 AND o.status='DISPATCHING' AND o.lease_owner=$2 AND o.fencing_token=$3 AND o.lease_expires_at>clock_timestamp() AND NOT EXISTS(SELECT 1 FROM control_states x WHERE x.active AND (x.scope_type IN ('GLOBAL_PAUSE','KILL_SWITCH') OR (x.scope_type='SYMBOL_PAUSE' AND x.scope_key=c.symbol) OR (x.scope_type='STRATEGY_PAUSE' AND x.scope_key=c.strategy))))");
        command.Parameters.AddWithValue(claim.OutboxId);command.Parameters.AddWithValue(claim.Owner);command.Parameters.AddWithValue(claim.FencingToken);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
    private static async Task<bool> ControlActiveAsync(NpgsqlConnection connection,NpgsqlTransaction transaction,Guid outboxId,CancellationToken cancellationToken)
    { await using var command=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM outbox_messages o LEFT JOIN workflow_cycles c ON c.cycle_id=o.cycle_id JOIN control_states x ON x.active AND (x.scope_type IN ('GLOBAL_PAUSE','KILL_SWITCH') OR (x.scope_type='SYMBOL_PAUSE' AND x.scope_key=c.symbol) OR (x.scope_type='STRATEGY_PAUSE' AND x.scope_key=c.strategy)) WHERE o.outbox_id=$1)",connection,transaction);command.Parameters.AddWithValue(outboxId);return (bool)(await command.ExecuteScalarAsync(cancellationToken))!; }
    private static DateTimeOffset ReadUtc(NpgsqlDataReader reader,int ordinal) => new(DateTime.SpecifyKind(reader.GetDateTime(ordinal),DateTimeKind.Utc));
    private static DateTimeOffset ToUtc(object? value) => new(DateTime.SpecifyKind((DateTime)(value??throw new InvalidOperationException("Database time was null.")),DateTimeKind.Utc));
}
