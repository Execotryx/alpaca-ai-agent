using AlpacaAgent.Application.Ports;
using AlpacaAgent.Application.Workflow;
using AlpacaAgent.Domain.Workflow;
using Npgsql;
using NpgsqlTypes;

namespace AlpacaAgent.Persistence;

public sealed class PostgresWorkflowKernel(NpgsqlDataSource dataSource) : IDurableWorkflowKernel
{
    public async Task<DurableScheduleResult> ScheduleAsync(DurableScheduleRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ScheduleKey);
        var template = request.Template == "REPLAY_SIMULATION"
            ? request.ReplaySourceTemplate ?? throw new ArgumentException("Replay requires its recorded source template.", nameof(request))
            : request.Template;
        var applicable = WorkflowCatalog.ApplicableNodes(template);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (await IsBlockedAsync(connection, transaction, request.Symbol, request.Strategy, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(Guid.Empty, false, "CONTROL_ACTIVE");
        }

        var cycleId = Guid.NewGuid();
        await using (var command = new NpgsqlCommand("""
INSERT INTO workflow_cycles(cycle_id,schedule_key,template,symbol,strategy,status,deadline)
VALUES($1,$2,$3,$4,$5,'RUNNING',$6)
ON CONFLICT(schedule_key) DO NOTHING
RETURNING cycle_id
""", connection, transaction))
        {
            command.Parameters.AddWithValue(cycleId); command.Parameters.AddWithValue(request.ScheduleKey);
            command.Parameters.AddWithValue(request.Template); command.Parameters.AddWithValue(request.Symbol); command.Parameters.AddWithValue((object?)request.Strategy ?? DBNull.Value); command.Parameters.AddWithValue(request.Deadline);
            var inserted = await command.ExecuteScalarAsync(cancellationToken);
            if (inserted is null)
            {
                await using var existing = new NpgsqlCommand("SELECT cycle_id FROM workflow_cycles WHERE schedule_key=$1", connection, transaction);
                existing.Parameters.AddWithValue(request.ScheduleKey);
                cycleId = (Guid)(await existing.ExecuteScalarAsync(cancellationToken))!;
                await transaction.CommitAsync(cancellationToken);
                return new(cycleId, false, "EXISTING");
            }
        }

        foreach (var node in WorkflowCatalog.AllNodes)
        {
            var applies = applicable.Contains(node);
            var status = node == WorkflowNode.StartCycle ? "COMPLETED" : applies ? "PENDING" : "SKIPPED";
            var reason = node == WorkflowNode.StartCycle ? "scheduler-created" : applies ? "awaiting-dependency" : $"not-applicable:{request.Template}";
            await using var step = new NpgsqlCommand("INSERT INTO workflow_steps(cycle_id,step_id,status,routing_reason,due_at,attempt_number) VALUES($1,$2,$3,$4,NULL,$5)", connection, transaction);
            step.Parameters.AddWithValue(cycleId); step.Parameters.AddWithValue((int)node); step.Parameters.AddWithValue(status); step.Parameters.AddWithValue(reason); step.Parameters.AddWithValue(node == WorkflowNode.StartCycle ? 1 : 0);
            await step.ExecuteNonQueryAsync(cancellationToken);
        }

        var now = await DatabaseNowAsync(connection, transaction, cancellationToken);
        var attemptId = Guid.NewGuid();
        await using (var start = new NpgsqlCommand("INSERT INTO workflow_step_attempts(attempt_id,cycle_id,step_id,attempt_number,owner,fencing_token,database_claim_time,input_references,component_versions) VALUES($1,$2,1,1,'SCHEDULER',1,$3,'[]'::jsonb,'{}'::jsonb)", connection, transaction))
        { start.Parameters.AddWithValue(attemptId); start.Parameters.AddWithValue(cycleId); start.Parameters.AddWithValue(now); await start.ExecuteNonQueryAsync(cancellationToken); }
        await using (var result = new NpgsqlCommand("INSERT INTO workflow_step_attempt_results(result_id,attempt_id,observed_at,authoritative,outcome,classification) VALUES($1,$2,$3,true,'COMPLETED','SCHEDULER_CREATED')", connection, transaction))
        { result.Parameters.AddWithValue(Guid.NewGuid()); result.Parameters.AddWithValue(attemptId); result.Parameters.AddWithValue(now); await result.ExecuteNonQueryAsync(cancellationToken); }
        var first = applicable.Where(node => node != WorkflowNode.StartCycle).MinBy(node => (int)node);
        await using (var activate = new NpgsqlCommand("UPDATE workflow_steps SET due_at=$3,routing_reason='dependency-satisfied' WHERE cycle_id=$1 AND step_id=$2", connection, transaction))
        { activate.Parameters.AddWithValue(cycleId); activate.Parameters.AddWithValue((int)first); activate.Parameters.AddWithValue(now); await activate.ExecuteNonQueryAsync(cancellationToken); }
        await transaction.CommitAsync(cancellationToken);
        return new(cycleId, true, "CREATED");
    }

    public async Task<DurableStepClaim?> ClaimDueStepAsync(string owner, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
WITH candidate AS (
 SELECT s.cycle_id,s.step_id FROM workflow_steps s JOIN workflow_cycles c ON c.cycle_id=s.cycle_id
 WHERE s.status='PENDING' AND s.due_at <= clock_timestamp() AND c.status='RUNNING' AND c.deadline > clock_timestamp()
 AND NOT EXISTS(SELECT 1 FROM control_states x WHERE x.active AND (x.scope_type IN ('GLOBAL_PAUSE','KILL_SWITCH') OR (x.scope_type='SYMBOL_PAUSE' AND x.scope_key=c.symbol) OR (x.scope_type='STRATEGY_PAUSE' AND x.scope_key=c.strategy)))
 AND NOT EXISTS(SELECT 1 FROM workflow_steps p WHERE p.cycle_id=s.cycle_id AND p.step_id<s.step_id AND p.status NOT IN ('COMPLETED','SKIPPED','SAFELY_DEFERRED','FAILED'))
 ORDER BY s.due_at,s.cycle_id,s.step_id FOR UPDATE OF s SKIP LOCKED LIMIT 1)
UPDATE workflow_steps s SET status='RUNNING',lease_owner=$1,lease_expires_at=clock_timestamp()+$2::interval,
 fencing_token=s.fencing_token+1,attempt_number=s.attempt_number+1,routing_reason='claimed'
FROM candidate c,workflow_cycles w WHERE s.cycle_id=c.cycle_id AND s.step_id=c.step_id AND w.cycle_id=s.cycle_id
RETURNING s.cycle_id,s.step_id,s.attempt_number,s.fencing_token,s.lease_expires_at,w.deadline,clock_timestamp()
""", connection, transaction);
        command.Parameters.AddWithValue(owner); command.Parameters.AddWithValue(leaseDuration);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) { await reader.DisposeAsync(); await transaction.CommitAsync(cancellationToken); return null; }
        var cycleId = reader.GetGuid(0); var stepId = reader.GetInt32(1); var attemptNumber = reader.GetInt32(2); var fence = reader.GetInt64(3);
        var expires = ReadUtc(reader,4); var deadline = ReadUtc(reader,5); var claimedAt = ReadUtc(reader,6);
        await reader.DisposeAsync();
        var attemptId = Guid.NewGuid();
        await using var start = new NpgsqlCommand("INSERT INTO workflow_step_attempts(attempt_id,cycle_id,step_id,attempt_number,owner,fencing_token,database_claim_time,input_references,component_versions) VALUES($1,$2,$3,$4,$5,$6,$7,'[]'::jsonb,'{}'::jsonb)", connection, transaction);
        start.Parameters.AddWithValue(attemptId); start.Parameters.AddWithValue(cycleId); start.Parameters.AddWithValue(stepId); start.Parameters.AddWithValue(attemptNumber); start.Parameters.AddWithValue(owner); start.Parameters.AddWithValue(fence); start.Parameters.AddWithValue(claimedAt);
        await start.ExecuteNonQueryAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return new(attemptId, cycleId, (WorkflowNode)stepId, attemptNumber, owner, fence, expires, deadline);
    }

    public async Task<DurableFinalizationResult> FinalizeAsync(DurableStepClaim claim, DurableStepCompletion completion, CancellationToken cancellationToken)
    {
        if ((completion.Outbox ?? []).Any(message => message.MessageType is not ("ALERT" or "RECORDER" or "TEST")))
            throw new InvalidOperationException("Phase 3 outbox cannot deliver broker-write messages.");
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        string status; string? owner; long fence; DateTimeOffset? lease; DateTimeOffset deadline; string symbol; string? strategy;
        await using (var current = new NpgsqlCommand("SELECT s.status,s.lease_owner,s.fencing_token,s.lease_expires_at,c.deadline,c.symbol,c.strategy FROM workflow_steps s JOIN workflow_cycles c ON c.cycle_id=s.cycle_id WHERE s.cycle_id=$1 AND s.step_id=$2 FOR UPDATE OF s", connection, transaction))
        {
            current.Parameters.AddWithValue(claim.CycleId); current.Parameters.AddWithValue((int)claim.Node);
            await using var reader = await current.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Claimed workflow step no longer exists.");
            status=reader.GetString(0); owner=reader.IsDBNull(1)?null:reader.GetString(1); fence=reader.GetInt64(2); lease=reader.IsDBNull(3)?null:ReadUtc(reader,3); deadline=ReadUtc(reader,4); symbol=reader.GetString(5); strategy=reader.IsDBNull(6)?null:reader.GetString(6);
        }
        var now = await DatabaseNowAsync(connection, transaction, cancellationToken);
        var controlActive = await IsBlockedAsync(connection, transaction, symbol, strategy, cancellationToken);
        var authority = status == "RUNNING" && owner == claim.Owner && fence == claim.FencingToken && lease > now && deadline > now && !(controlActive && (completion.Outbox?.Count > 0));
        var classification = authority ? completion.Classification : controlActive ? "CONTROL_ACTIVE" : deadline <= now ? "DEADLINE_EXPIRED" : lease <= now ? "LEASE_EXPIRED" : "LOST_AUTHORITY";
        await using (var result = new NpgsqlCommand("INSERT INTO workflow_step_attempt_results(result_id,attempt_id,observed_at,authoritative,outcome,classification,next_due_at,output_references,diagnostic_reference) VALUES($1,$2,$3,$4,$5,$6,$7,$8::jsonb,$9)", connection, transaction))
        {
            result.Parameters.AddWithValue(Guid.NewGuid()); result.Parameters.AddWithValue(claim.AttemptId); result.Parameters.AddWithValue(now); result.Parameters.AddWithValue(authority);
            result.Parameters.AddWithValue(completion.Outcome); result.Parameters.AddWithValue(classification); result.Parameters.AddWithValue((object?)completion.NextDueAt ?? DBNull.Value);
            result.Parameters.AddWithValue(completion.OutputReferencesJson); result.Parameters.AddWithValue((object?)completion.DiagnosticReference ?? DBNull.Value); await result.ExecuteNonQueryAsync(cancellationToken);
        }
        if (!authority) { await transaction.CommitAsync(cancellationToken); return new(false, false, classification); }
        var retrying = completion.NextDueAt is not null;
        var stepState = retrying ? "PENDING" : completion.Outcome switch { "COMPLETED" or "NO_ACTION" or "WAITING" => "COMPLETED", "SAFELY_DEFERRED" => "SAFELY_DEFERRED", _ => "FAILED" };
        await using (var update = new NpgsqlCommand("UPDATE workflow_steps SET status=$3,routing_reason=$4,output_references=$5::jsonb,lease_owner=NULL,lease_expires_at=NULL WHERE cycle_id=$1 AND step_id=$2", connection, transaction))
        { update.Parameters.AddWithValue(claim.CycleId); update.Parameters.AddWithValue((int)claim.Node); update.Parameters.AddWithValue(stepState); update.Parameters.AddWithValue(classification); update.Parameters.AddWithValue(completion.OutputReferencesJson); await update.ExecuteNonQueryAsync(cancellationToken); }
        if (retrying)
        {
            await using var retry = new NpgsqlCommand("UPDATE workflow_steps SET due_at=$3 WHERE cycle_id=$1 AND step_id=$2", connection, transaction);
            retry.Parameters.AddWithValue(claim.CycleId); retry.Parameters.AddWithValue((int)claim.Node); retry.Parameters.AddWithValue(completion.NextDueAt!.Value);
            await retry.ExecuteNonQueryAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return new(true, false, classification);
        }
        foreach (var message in completion.Outbox ?? [])
        {
            await using var outbox = new NpgsqlCommand("INSERT INTO outbox_messages(outbox_id,deduplication_key,message_type,message_version,cycle_id,payload,status,due_at,maximum_attempts) VALUES($1,$2,$3,$4,$5,$6::jsonb,'PENDING',$7,$8) ON CONFLICT(deduplication_key) DO NOTHING", connection, transaction);
            outbox.Parameters.AddWithValue(message.Id); outbox.Parameters.AddWithValue(message.DeduplicationKey); outbox.Parameters.AddWithValue(message.MessageType); outbox.Parameters.AddWithValue(message.MessageVersion); outbox.Parameters.AddWithValue(claim.CycleId); outbox.Parameters.AddWithValue(message.PayloadJson); outbox.Parameters.AddWithValue(message.DueAt); outbox.Parameters.AddWithValue(message.MaximumAttempts); await outbox.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach (var checkpoint in completion.Checkpoints ?? [])
        {
            await using var command = new NpgsqlCommand("INSERT INTO workflow_checkpoints(checkpoint_id,cycle_id,step_id,content_hash,contract_version,payload_reference) VALUES($1,$2,$3,$4,$5,$6) ON CONFLICT(cycle_id,step_id,content_hash,contract_version) DO NOTHING", connection, transaction);
            command.Parameters.AddWithValue(checkpoint.Id); command.Parameters.AddWithValue(claim.CycleId); command.Parameters.AddWithValue((int)claim.Node); command.Parameters.AddWithValue(checkpoint.ContentHash); command.Parameters.AddWithValue(checkpoint.ContractVersion); command.Parameters.AddWithValue(checkpoint.PayloadReference);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach (var domainEvent in completion.DomainEvents ?? [])
        {
            await using var command = new NpgsqlCommand("INSERT INTO domain_events(event_id,correlation_id,cycle_id,event_type,contract_version,payload,occurred_at) VALUES($1,$2,$3,$4,$5,$6::jsonb,$7) ON CONFLICT(event_id) DO NOTHING", connection, transaction);
            command.Parameters.AddWithValue(domainEvent.Id); command.Parameters.AddWithValue(domainEvent.CorrelationId); command.Parameters.AddWithValue(claim.CycleId); command.Parameters.AddWithValue(domainEvent.EventType); command.Parameters.AddWithValue(domainEvent.ContractVersion); command.Parameters.AddWithValue(domainEvent.PayloadJson); command.Parameters.AddWithValue(domainEvent.OccurredAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        var terminalRoute = completion.Outcome is not ("COMPLETED" or "WAITING");
        if (terminalRoute)
        {
            await using var skip = new NpgsqlCommand("UPDATE workflow_steps SET status='SKIPPED',routing_reason='terminal-route:'||$2 WHERE cycle_id=$1 AND status='PENDING'", connection, transaction);
            skip.Parameters.AddWithValue(claim.CycleId); skip.Parameters.AddWithValue(classification); await skip.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await using var activate = new NpgsqlCommand("UPDATE workflow_steps SET due_at=$2,routing_reason='dependency-satisfied' WHERE cycle_id=$1 AND step_id=(SELECT min(step_id) FROM workflow_steps WHERE cycle_id=$1 AND status='PENDING')", connection, transaction);
            activate.Parameters.AddWithValue(claim.CycleId); activate.Parameters.AddWithValue(now); await activate.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var finish = new NpgsqlCommand("UPDATE workflow_cycles SET status=CASE WHEN NOT EXISTS(SELECT 1 FROM workflow_steps WHERE cycle_id=$1 AND status IN ('PENDING','RUNNING')) THEN CASE $2 WHEN 'COMPLETED' THEN 'COMPLETED' WHEN 'WAITING' THEN 'COMPLETED' WHEN 'NO_ACTION' THEN 'NO_ACTION' WHEN 'SAFELY_DEFERRED' THEN 'SAFELY_DEFERRED' ELSE 'FAILED_AND_ESCALATED' END ELSE status END WHERE cycle_id=$1", connection, transaction))
        { finish.Parameters.AddWithValue(claim.CycleId); finish.Parameters.AddWithValue(completion.Outcome); await finish.ExecuteNonQueryAsync(cancellationToken); }
        await transaction.CommitAsync(cancellationToken); return new(true, true, classification);
    }

    public async Task<int> RecoverExpiredLeasesAsync(int maximumAttempts, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var expired = new List<(Guid CycleId, int StepId, int AttemptNumber, Guid AttemptId)>();
        await using (var select = new NpgsqlCommand("""
SELECT s.cycle_id,s.step_id,s.attempt_number,a.attempt_id FROM workflow_steps s
JOIN workflow_step_attempts a ON a.cycle_id=s.cycle_id AND a.step_id=s.step_id AND a.attempt_number=s.attempt_number
WHERE s.status='RUNNING' AND s.lease_expires_at <= clock_timestamp() FOR UPDATE OF s SKIP LOCKED
""", connection, transaction))
        await using (var reader = await select.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken)) expired.Add((reader.GetGuid(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetGuid(3)));
        var now = await DatabaseNowAsync(connection, transaction, cancellationToken);
        foreach (var item in expired)
        {
            await using (var result = new NpgsqlCommand("INSERT INTO workflow_step_attempt_results(result_id,attempt_id,observed_at,authoritative,outcome,classification) VALUES($1,$2,$3,true,'LOST_LEASE','LEASE_EXPIRED') ON CONFLICT (attempt_id) WHERE authoritative DO NOTHING", connection, transaction))
            { result.Parameters.AddWithValue(Guid.NewGuid()); result.Parameters.AddWithValue(item.AttemptId); result.Parameters.AddWithValue(now); await result.ExecuteNonQueryAsync(cancellationToken); }
            await using var update = new NpgsqlCommand("UPDATE workflow_steps SET status=$3,due_at=$4,lease_owner=NULL,lease_expires_at=NULL,routing_reason=$5 WHERE cycle_id=$1 AND step_id=$2", connection, transaction);
            var retry = item.AttemptNumber < maximumAttempts;
            update.Parameters.AddWithValue(item.CycleId); update.Parameters.AddWithValue(item.StepId); update.Parameters.AddWithValue(retry ? "PENDING" : "FAILED");
            update.Parameters.AddWithValue(retry ? now : DBNull.Value); update.Parameters.AddWithValue(retry ? "expired-lease-retry" : "expired-lease-budget-exhausted");
            await update.ExecuteNonQueryAsync(cancellationToken);
            if (!retry)
            {
                await using (var skip = new NpgsqlCommand("UPDATE workflow_steps SET status='SKIPPED',routing_reason='terminal-route:expired-lease-budget-exhausted' WHERE cycle_id=$1 AND status='PENDING'", connection, transaction))
                { skip.Parameters.AddWithValue(item.CycleId); await skip.ExecuteNonQueryAsync(cancellationToken); }
                await using var failCycle = new NpgsqlCommand("UPDATE workflow_cycles SET status='FAILED_AND_ESCALATED',terminal_reason='expired-lease-budget-exhausted' WHERE cycle_id=$1 AND status='RUNNING'", connection, transaction);
                failCycle.Parameters.AddWithValue(item.CycleId); await failCycle.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        await transaction.CommitAsync(cancellationToken);
        return expired.Count;
    }

    public async Task SetControlAsync(ControlChange change, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken); await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var id = Guid.NewGuid(); bool? prior = null;
        await using (var command = new NpgsqlCommand("SELECT control_id,active FROM control_states WHERE scope_type=$1 AND scope_key=$2 FOR UPDATE", connection, transaction))
        { command.Parameters.AddWithValue(change.ScopeType); command.Parameters.AddWithValue(change.ScopeKey); await using var reader=await command.ExecuteReaderAsync(cancellationToken); if(await reader.ReadAsync(cancellationToken)){id=reader.GetGuid(0);prior=reader.GetBoolean(1);} }
        await using (var upsert = new NpgsqlCommand("INSERT INTO control_states(control_id,scope_type,scope_key,active,reason,actor) VALUES($1,$2,$3,$4,$5,$6) ON CONFLICT(scope_type,scope_key) DO UPDATE SET active=EXCLUDED.active,reason=EXCLUDED.reason,actor=EXCLUDED.actor,changed_at=clock_timestamp()", connection, transaction))
        { upsert.Parameters.AddWithValue(id); upsert.Parameters.AddWithValue(change.ScopeType); upsert.Parameters.AddWithValue(change.ScopeKey); upsert.Parameters.AddWithValue(change.Active); upsert.Parameters.AddWithValue(change.Reason); upsert.Parameters.AddWithValue(change.Actor); await upsert.ExecuteNonQueryAsync(cancellationToken); }
        await using (var audit = new NpgsqlCommand("INSERT INTO control_state_events(event_id,control_id,prior_value,new_value,actor,source,reason) VALUES($1,$2,$3,$4,$5,$6,$7)", connection, transaction))
        { audit.Parameters.AddWithValue(Guid.NewGuid()); audit.Parameters.AddWithValue(id); audit.Parameters.AddWithValue((object?)prior??DBNull.Value); audit.Parameters.AddWithValue(change.Active); audit.Parameters.AddWithValue(change.Actor); audit.Parameters.AddWithValue(change.Source); audit.Parameters.AddWithValue(change.Reason); await audit.ExecuteNonQueryAsync(cancellationToken); }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<WorkflowHealth> ObserveHealthAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("SELECT (SELECT EXTRACT(EPOCH FROM clock_timestamp()-max(database_claim_time))::bigint FROM workflow_step_attempts WHERE owner<>'SCHEDULER'),COALESCE(EXTRACT(EPOCH FROM clock_timestamp()-min(due_at))::bigint,0),count(*) FILTER(WHERE status='RUNNING' AND lease_expires_at<=clock_timestamp()),(SELECT count(*) FROM outbox_messages WHERE status='PENDING'),(SELECT COALESCE(EXTRACT(EPOCH FROM clock_timestamp()-min(due_at))::bigint,0) FROM outbox_messages WHERE status='PENDING'),(SELECT count(*) FROM workflow_steps WHERE attempt_number>1)+(SELECT count(*) FROM outbox_messages WHERE attempt_count>1),(SELECT count(*) FROM outbox_messages WHERE status='DEAD_LETTER'),(SELECT count(*) FROM workflow_step_attempt_results WHERE classification IN ('LOST_AUTHORITY','LEASE_EXPIRED'))+(SELECT count(*) FROM outbox_dispatch_attempt_results WHERE classification IN ('LOST_AUTHORITY','LEASE_EXPIRED')),EXISTS(SELECT 1 FROM control_states WHERE scope_type='KILL_SWITCH' AND active) FROM workflow_steps WHERE status IN ('PENDING','RUNNING')");
        await using var reader=await command.ExecuteReaderAsync(cancellationToken); await reader.ReadAsync(cancellationToken);
        return new(true, reader.IsDBNull(0)?null:reader.GetInt64(0), reader.GetInt64(1), checked((int)reader.GetInt64(2)), checked((int)reader.GetInt64(3)), reader.GetInt64(4), checked((int)reader.GetInt64(5)), checked((int)reader.GetInt64(6)), checked((int)reader.GetInt64(7)), reader.GetBoolean(8));
    }

    private static async Task<DateTimeOffset> DatabaseNowAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    { await using var command=new NpgsqlCommand("SELECT clock_timestamp()",connection,transaction); return ToUtc(await command.ExecuteScalarAsync(cancellationToken)); }
    private static DateTimeOffset ReadUtc(NpgsqlDataReader reader,int ordinal) => new(DateTime.SpecifyKind(reader.GetDateTime(ordinal),DateTimeKind.Utc));
    private static DateTimeOffset ToUtc(object? value) => new(DateTime.SpecifyKind((DateTime)(value??throw new InvalidOperationException("Database time was null.")),DateTimeKind.Utc));
    private static async Task<bool> IsBlockedAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string symbol, string? strategy, CancellationToken cancellationToken)
    { await using var command=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM control_states WHERE active AND (scope_type IN ('GLOBAL_PAUSE','KILL_SWITCH') OR (scope_type='SYMBOL_PAUSE' AND scope_key=$1) OR (scope_type='STRATEGY_PAUSE' AND scope_key=$2)))",connection,transaction); command.Parameters.AddWithValue(symbol); command.Parameters.AddWithValue((object?)strategy??DBNull.Value); return (bool)(await command.ExecuteScalarAsync(cancellationToken))!; }
}
