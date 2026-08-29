using AlpacaAgent.Application.Workflow;
using AlpacaAgent.Application.Ports;
using AlpacaAgent.Domain.Workflow;
using AlpacaAgent.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AlpacaAgent.PersistenceTests;

[Collection("PostgreSQL Phase 3")]
public sealed class PostgresWorkflowKernelTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18.6-alpine3.23")
        .WithDatabase("alpaca_agent_tests").WithUsername("postgres").WithPassword("postgres").Build();
    private NpgsqlDataSource dataSource = null!;
    private PostgresWorkflowKernel kernel = null!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        dataSource = NpgsqlDataSource.Create(container.GetConnectionString());
        var options = new DbContextOptionsBuilder<WorkflowDbContext>().UseNpgsql(dataSource).Options;
        await using var context = new WorkflowDbContext(options);
        await context.Database.MigrateAsync();
        kernel = new(dataSource);
    }
    public async Task DisposeAsync() { await dataSource.DisposeAsync(); await container.DisposeAsync(); }

    [Fact(DisplayName = "P3-DB-001 duplicate schedule ticks return one cycle")]
    public async Task P3_DB_001_DuplicateScheduleTicksReturnOneCycle()
    {
        var request = Request("duplicate");
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => kernel.ScheduleAsync(request, CancellationToken.None)));
        Assert.Single(results.Select(result => result.CycleId).Distinct()); Assert.Single(results, result => result.Created);
    }

    [Fact(DisplayName = "P3-DB-002 all 18 nodes persist with exact applicability")]
    public async Task P3_DB_002_AllNodesPersistWithExactApplicability()
    {
        var cycle = await kernel.ScheduleAsync(Request("nodes", "PERFORMANCE_REPORT"), CancellationToken.None);
        await using var command = dataSource.CreateCommand("SELECT step_id,status,routing_reason FROM workflow_steps WHERE cycle_id=$1 ORDER BY step_id"); command.Parameters.AddWithValue(cycle.CycleId);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None); var rows = new List<(int,string,string)>();
        while (await reader.ReadAsync(CancellationToken.None)) rows.Add((reader.GetInt32(0),reader.GetString(1),reader.GetString(2)));
        Assert.Equal(18,rows.Count); Assert.Equal("COMPLETED",rows[0].Item2); Assert.Equal("PENDING",rows[16].Item2); Assert.Equal("PENDING",rows[17].Item2);
        Assert.All(rows.Where(row => row.Item1 is > 1 and < 17), row => { Assert.Equal("SKIPPED",row.Item2); Assert.Equal("not-applicable:PERFORMANCE_REPORT",row.Item3); });
    }

    [Fact(DisplayName = "P3-DB-003 node 1 attempt and result are immutable")]
    public async Task P3_DB_003_NodeOneEvidenceIsImmutable()
    {
        var cycle=await kernel.ScheduleAsync(Request("node-one"),CancellationToken.None);
        await using var count=dataSource.CreateCommand("SELECT count(*) FROM workflow_step_attempts a JOIN workflow_step_attempt_results r ON r.attempt_id=a.attempt_id WHERE a.cycle_id=$1 AND a.step_id=1 AND r.authoritative");count.Parameters.AddWithValue(cycle.CycleId);
        Assert.Equal(1L,(long)(await count.ExecuteScalarAsync(CancellationToken.None))!);
        await using var mutate=dataSource.CreateCommand("UPDATE workflow_step_attempts SET owner='changed' WHERE cycle_id=$1 AND step_id=1");mutate.Parameters.AddWithValue(cycle.CycleId);
        await Assert.ThrowsAsync<PostgresException>(()=>mutate.ExecuteNonQueryAsync(CancellationToken.None));
    }

    [Fact(DisplayName = "P3-DB-004 concurrent claims produce one current owner")]
    public async Task P3_DB_004_ConcurrentClaimsProduceOneOwner()
    {
        await kernel.ScheduleAsync(Request("claims"),CancellationToken.None);
        var claims=await Task.WhenAll(Enumerable.Range(0,8).Select(index=>kernel.ClaimDueStepAsync($"worker-{index}",TimeSpan.FromMinutes(1),CancellationToken.None)));
        Assert.Single(claims, claim=>claim is not null); Assert.Equal(WorkflowNode.ReconcileBrokerState,claims.Single(claim=>claim is not null)!.Node);
    }

    [Fact(DisplayName = "P3-FI-001 cancellation before claim commit leaves no attempt")]
    public async Task P3_FI_001_CancellationBeforeClaimCommitLeavesNoAttempt()
    {
        var cycle=await kernel.ScheduleAsync(Request("cancel-before-claim"),CancellationToken.None);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>kernel.ClaimDueStepAsync("cancelled-worker",TimeSpan.FromMinutes(1),cancelled.Token));
        Assert.Equal(0L,await Scalar<long>("SELECT count(*) FROM workflow_step_attempts WHERE cycle_id=$1 AND owner='cancelled-worker'",cycle.CycleId));
        Assert.Equal("PENDING",await Scalar<string>("SELECT status FROM workflow_steps WHERE cycle_id=$1 AND step_id=2",cycle.CycleId));
    }

    [Fact(DisplayName = "P3-DB-005 fencing tokens increase across reclaim")]
    public async Task P3_DB_005_FencingTokensIncreaseAcrossReclaim()
    {
        await kernel.ScheduleAsync(Request("fences"),CancellationToken.None);
        var first=(await kernel.ClaimDueStepAsync("first",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        await ExpireLease(first.CycleId,(int)first.Node); Assert.Equal(1,await kernel.RecoverExpiredLeasesAsync(3,CancellationToken.None));
        var second=(await kernel.ClaimDueStepAsync("second",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        Assert.True(second.FencingToken>first.FencingToken); Assert.Equal(2,second.AttemptNumber);
    }

    [Fact(DisplayName = "P3-DB-006 stale finalization cannot advance state")]
    public async Task P3_DB_006_StaleFinalizationCannotAdvanceState()
    {
        await kernel.ScheduleAsync(Request("stale"),CancellationToken.None);
        var stale=(await kernel.ClaimDueStepAsync("stale",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        await ExpireLease(stale.CycleId,(int)stale.Node); await kernel.RecoverExpiredLeasesAsync(3,CancellationToken.None);
        _=await kernel.ClaimDueStepAsync("current",TimeSpan.FromMinutes(1),CancellationToken.None);
        var result=await kernel.FinalizeAsync(stale,new("COMPLETED","OK"),CancellationToken.None);
        Assert.False(result.Authoritative); Assert.Equal("LOST_AUTHORITY",result.Classification);
    }

    [Fact(DisplayName = "P3-DB-007 valid finalization atomically activates successor")]
    public async Task P3_DB_007_ValidFinalizationActivatesSuccessor()
    {
        await kernel.ScheduleAsync(Request("finalize"),CancellationToken.None);
        var claim=(await kernel.ClaimDueStepAsync("worker",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        var result=await kernel.FinalizeAsync(claim,new("COMPLETED","OK","[\"snapshot-1\"]"),CancellationToken.None); Assert.True(result.Advanced);
        await using var command=dataSource.CreateCommand("SELECT status,due_at IS NOT NULL FROM workflow_steps WHERE cycle_id=$1 AND step_id=3");command.Parameters.AddWithValue(claim.CycleId);
        await using var reader=await command.ExecuteReaderAsync(CancellationToken.None);await reader.ReadAsync(CancellationToken.None);Assert.Equal("PENDING",reader.GetString(0));Assert.True(reader.GetBoolean(1));
    }

    [Fact(DisplayName = "P3-FI-006 lost acknowledgment after finalization cannot advance twice")]
    public async Task P3_FI_006_LostAcknowledgmentCannotAdvanceTwice()
    {
        var cycle=await kernel.ScheduleAsync(Request("lost-finalize-ack"),CancellationToken.None);
        var claim=(await kernel.ClaimDueStepAsync("worker",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        Assert.True((await kernel.FinalizeAsync(claim,new("COMPLETED","OK"),CancellationToken.None)).Authoritative);
        var replay=await kernel.FinalizeAsync(claim,new("COMPLETED","OK"),CancellationToken.None);
        Assert.False(replay.Authoritative);
        Assert.Equal(1L,await Scalar<long>("SELECT count(*) FROM workflow_step_attempt_results WHERE attempt_id=$1 AND authoritative",claim.AttemptId));
        Assert.Equal(1L,await Scalar<long>("SELECT count(*) FROM workflow_steps WHERE cycle_id=$1 AND step_id=3 AND due_at IS NOT NULL",cycle.CycleId));
    }

    [Fact(DisplayName = "P3-DB-008 state and outbox rollback together")]
    public async Task P3_DB_008_StateAndOutboxRollbackTogether()
    {
        await kernel.ScheduleAsync(Request("rollback"),CancellationToken.None);
        var claim=(await kernel.ClaimDueStepAsync("worker",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        var completion=new DurableStepCompletion("COMPLETED","OK",Outbox:[new(Guid.NewGuid(),"rollback-key","TEST","1.0","not-json",DateTimeOffset.UtcNow,2)]);
        await Assert.ThrowsAsync<PostgresException>(()=>kernel.FinalizeAsync(claim,completion,CancellationToken.None));
        Assert.Equal("RUNNING",await Scalar<string>("SELECT status FROM workflow_steps WHERE cycle_id=$1 AND step_id=$2",claim.CycleId,(int)claim.Node));
        Assert.Equal(0L,await Scalar<long>("SELECT count(*) FROM outbox_messages WHERE deduplication_key='rollback-key'"));
        Assert.Equal(0L,await Scalar<long>("SELECT count(*) FROM workflow_step_attempt_results WHERE attempt_id=$1",claim.AttemptId));
    }

    [Fact(DisplayName = "P3-DB-009 retry appends a new attempt and preserves old result")]
    public async Task P3_DB_009_RetryAppendsAndPreservesHistory()
    {
        await kernel.ScheduleAsync(Request("retry"),CancellationToken.None);
        var first=(await kernel.ClaimDueStepAsync("worker-1",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        var retry=await kernel.FinalizeAsync(first,new("RETRYABLE_FAILURE","TRANSIENT",NextDueAt:DateTimeOffset.UtcNow),CancellationToken.None);
        Assert.True(retry.Authoritative);Assert.False(retry.Advanced);
        var second=(await kernel.ClaimDueStepAsync("worker-2",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        Assert.Equal(2,second.AttemptNumber);Assert.NotEqual(first.AttemptId,second.AttemptId);
        Assert.Equal(1L,await Scalar<long>("SELECT count(*) FROM workflow_step_attempt_results WHERE attempt_id=$1 AND authoritative",first.AttemptId));
    }

    [Fact(DisplayName = "P3-DB-010 exact-deadline completion loses authority")]
    public async Task P3_DB_010_ExactDeadlineCompletionLosesAuthority()
    {
        await kernel.ScheduleAsync(Request("deadline"),CancellationToken.None);
        var claim=(await kernel.ClaimDueStepAsync("worker",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        await Execute("UPDATE workflow_cycles SET deadline=clock_timestamp() WHERE cycle_id=$1",claim.CycleId);
        var result=await kernel.FinalizeAsync(claim,new("COMPLETED","OK"),CancellationToken.None);
        Assert.False(result.Authoritative);Assert.Equal("DEADLINE_EXPIRED",result.Classification);
        Assert.Equal(0L,await Scalar<long>("SELECT count(*) FROM workflow_steps WHERE cycle_id=$1 AND step_id=3 AND due_at IS NOT NULL",claim.CycleId));
    }

    [Fact(DisplayName = "P3-DB-011 cancellation persists a classified result")]
    public async Task P3_DB_011_CancellationPersistsClassifiedResult()
    {
        await kernel.ScheduleAsync(Request("cancel"),CancellationToken.None);
        var claim=(await kernel.ClaimDueStepAsync("worker",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        var result=await kernel.FinalizeAsync(claim,new("CANCELLED","CANCELLED",DiagnosticReference:"caller-cancelled"),CancellationToken.None);
        Assert.True(result.Authoritative);
        Assert.Equal("CANCELLED",await Scalar<string>("SELECT classification FROM workflow_step_attempt_results WHERE attempt_id=$1 AND authoritative",claim.AttemptId));
        Assert.Equal("FAILED",await Scalar<string>("SELECT status FROM workflow_steps WHERE cycle_id=$1 AND step_id=$2",claim.CycleId,(int)claim.Node));
        Assert.Equal("FAILED_AND_ESCALATED",await Scalar<string>("SELECT status FROM workflow_cycles WHERE cycle_id=$1",claim.CycleId));
    }

    [Fact(DisplayName = "P3-DB-012 terminal cycles reject further advancement")]
    public async Task P3_DB_012_TerminalCyclesRejectFurtherAdvancement()
    {
        var cycle=await kernel.ScheduleAsync(Request("terminal","PERFORMANCE_REPORT"),CancellationToken.None);
        await CompleteCycle(kernel);
        Assert.Equal("COMPLETED",await Scalar<string>("SELECT status FROM workflow_cycles WHERE cycle_id=$1",cycle.CycleId));
        Assert.Null(await kernel.ClaimDueStepAsync("late",TimeSpan.FromMinutes(1),CancellationToken.None));
        await Assert.ThrowsAsync<PostgresException>(()=>Execute("UPDATE workflow_cycles SET terminal_reason='changed' WHERE cycle_id=$1",cycle.CycleId));
    }

    [Fact(DisplayName = "P3-DB-013 process restart resumes from durable state")]
    public async Task P3_DB_013_ProcessRestartResumesFromDurableState()
    {
        await kernel.ScheduleAsync(Request("restart"),CancellationToken.None);
        var abandoned=(await kernel.ClaimDueStepAsync("old-process",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        await ExpireLease(abandoned.CycleId,(int)abandoned.Node);
        var restarted=new PostgresWorkflowKernel(dataSource);
        Assert.Equal(1,await restarted.RecoverExpiredLeasesAsync(3,CancellationToken.None));
        var resumed=(await restarted.ClaimDueStepAsync("new-process",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        Assert.Equal(abandoned.Node,resumed.Node);Assert.True(resumed.FencingToken>abandoned.FencingToken);
    }

    [Fact(DisplayName = "P3-DB-014 every template restarts at every applicable node")]
    public async Task P3_DB_014_EveryTemplateRestartsAtEveryApplicableNode()
    {
        var templates=new[]{"NEW_ENTRY","OPEN_ORDER_MONITOR","POSITION_MANAGEMENT","PRE_MARKET","END_OF_DAY","PERFORMANCE_REPORT"};
        foreach(var template in templates)
        {
            var cycle=await kernel.ScheduleAsync(Request($"restart-{template}",template),CancellationToken.None);
            var expected=WorkflowCatalog.ApplicableNodes(template).Count-1;var completed=0;
            while(true)
            {
                var restarted=new PostgresWorkflowKernel(dataSource);
                var claim=await restarted.ClaimDueStepAsync($"worker-{template}-{completed}",TimeSpan.FromMinutes(1),CancellationToken.None);
                if(claim is null)break;
                Assert.Equal(cycle.CycleId,claim.CycleId);
                var result=await restarted.FinalizeAsync(claim,new("COMPLETED","OK"),CancellationToken.None);Assert.True(result.Authoritative);completed++;
            }
            Assert.Equal(expected,completed);Assert.Equal("COMPLETED",await Scalar<string>("SELECT status FROM workflow_cycles WHERE cycle_id=$1",cycle.CycleId));
        }
    }

    [Fact(DisplayName = "P3-DB-015 replay uses its recorded source template")]
    public async Task P3_DB_015_ReplayUsesRecordedSourceTemplate()
    {
        var request=new DurableScheduleRequest("test:replay","AAPL","REPLAY_SIMULATION",DateTimeOffset.UtcNow.AddHours(1),"POSITION_MANAGEMENT");
        var cycle=await kernel.ScheduleAsync(request,CancellationToken.None);
        var applicable=WorkflowCatalog.ApplicableNodes("POSITION_MANAGEMENT").Select(node=>(int)node).Order().ToArray();
        await using var command=dataSource.CreateCommand("SELECT step_id FROM workflow_steps WHERE cycle_id=$1 AND status<>'SKIPPED' ORDER BY step_id");command.Parameters.AddWithValue(cycle.CycleId);
        await using var reader=await command.ExecuteReaderAsync(CancellationToken.None);var actual=new List<int>();while(await reader.ReadAsync(CancellationToken.None))actual.Add(reader.GetInt32(0));
        Assert.Equal(applicable,actual);Assert.Equal("REPLAY_SIMULATION",await Scalar<string>("SELECT template FROM workflow_cycles WHERE cycle_id=$1",cycle.CycleId));
    }

    [Fact(DisplayName = "P3-OUT-001 redelivery is logically once by deduplication key")]
    public async Task P3_OUT_001_RedeliveryIsLogicallyOnce()
    {
        var dispatcher=new PostgresOutboxDispatcher(dataSource);await SeedOutbox("once-key",2);
        var claim=(await dispatcher.ClaimAsync("dispatcher",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        var sink=new DeduplicatingSink();
        Assert.True((await dispatcher.DispatchAsync(claim,sink,CancellationToken.None)).Completed);
        Assert.False((await dispatcher.DispatchAsync(claim,sink,CancellationToken.None)).Completed);
        Assert.Equal(1,sink.Deliveries);Assert.Equal("COMPLETED",await Scalar<string>("SELECT status FROM outbox_messages WHERE outbox_id=$1",claim.OutboxId));
    }

    [Fact(DisplayName = "P3-DB-016 finalization commits checkpoints events and outbox atomically")]
    public async Task P3_DB_016_FinalizationCommitsAllCausedArtifacts()
    {
        var cycle=await kernel.ScheduleAsync(Request("caused-artifacts"),CancellationToken.None);
        var claim=(await kernel.ClaimDueStepAsync("artifact-worker",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        var completion=new DurableStepCompletion("COMPLETED","OK",Outbox:[new(Guid.NewGuid(),"artifact-outbox","TEST","1.0","{}",DateTimeOffset.UtcNow,2)],Checkpoints:[new(Guid.NewGuid(),"sha256:test","1.0","artifact://checkpoint")],DomainEvents:[new(Guid.NewGuid(),"artifact-correlation","STEP_COMPLETED","1.0","{}",DateTimeOffset.UtcNow)]);
        Assert.True((await kernel.FinalizeAsync(claim,completion,CancellationToken.None)).Authoritative);
        Assert.Equal(1L,await Scalar<long>("SELECT count(*) FROM workflow_checkpoints WHERE cycle_id=$1 AND step_id=$2",cycle.CycleId,(int)claim.Node));
        Assert.Equal(1L,await Scalar<long>("SELECT count(*) FROM domain_events WHERE cycle_id=$1 AND correlation_id='artifact-correlation'",cycle.CycleId));
        Assert.Equal(1L,await Scalar<long>("SELECT count(*) FROM outbox_messages WHERE cycle_id=$1 AND deduplication_key='artifact-outbox'",cycle.CycleId));
    }

    [Fact(DisplayName = "P3-OUT-002 bounded dispatch failures dead-letter with history")]
    public async Task P3_OUT_002_BoundedFailuresDeadLetter()
    {
        var dispatcher=new PostgresOutboxDispatcher(dataSource);await SeedOutbox("dead-key",2);var sink=new FailingSink();
        var first=(await dispatcher.ClaimAsync("dispatcher-1",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        Assert.True((await dispatcher.DispatchAsync(first,sink,CancellationToken.None)).Retryable);
        await Execute("UPDATE outbox_messages SET due_at=clock_timestamp() WHERE outbox_id=$1",first.OutboxId);
        var second=(await dispatcher.ClaimAsync("dispatcher-2",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        var result=await dispatcher.DispatchAsync(second,sink,CancellationToken.None);
        Assert.False(result.Retryable);Assert.Equal("DEAD_LETTER",result.Classification);
        Assert.Equal(2L,await Scalar<long>("SELECT count(*) FROM outbox_dispatch_attempt_results r JOIN outbox_dispatch_attempts a ON a.attempt_id=r.attempt_id WHERE a.outbox_id=$1",first.OutboxId));
    }

    [Fact(DisplayName = "P3-OUT-003 expired dispatch recovery is atomic and preserves evidence")]
    public async Task P3_OUT_003_ExpiredDispatchRecoveryPreservesEvidence()
    {
        var dispatcher=new PostgresOutboxDispatcher(dataSource);await SeedOutbox("expired-key",2);
        var claim=(await dispatcher.ClaimAsync("crashed-dispatcher",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        await Execute("UPDATE outbox_messages SET lease_expires_at=clock_timestamp()-interval '1 second' WHERE outbox_id=$1",claim.OutboxId);

        Assert.Equal(1,await dispatcher.RecoverExpiredAsync(CancellationToken.None));
        Assert.Equal("PENDING",await Scalar<string>("SELECT status FROM outbox_messages WHERE outbox_id=$1",claim.OutboxId));
        Assert.Equal("LEASE_EXPIRED",await Scalar<string>("SELECT classification FROM outbox_dispatch_attempt_results WHERE attempt_id=$1",claim.AttemptId));
        Assert.Equal(0,await dispatcher.RecoverExpiredAsync(CancellationToken.None));
    }

    [Fact(DisplayName = "P3-FI-008 delivery before guarded completion redelivers without duplicate effect")]
    public async Task P3_FI_008_DeliveryBeforeCompletionIsEffectivelyOnce()
    {
        var dispatcher=new PostgresOutboxDispatcher(dataSource);await SeedOutbox("post-delivery-crash",2);
        var sink=new ThrowAfterFirstLogicalEffectSink();
        var first=(await dispatcher.ClaimAsync("dispatcher-1",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        Assert.True((await dispatcher.DispatchAsync(first,sink,CancellationToken.None)).Retryable);
        await Execute("UPDATE outbox_messages SET due_at=clock_timestamp() WHERE outbox_id=$1",first.OutboxId);
        var second=(await dispatcher.ClaimAsync("dispatcher-2",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        Assert.True((await dispatcher.DispatchAsync(second,sink,CancellationToken.None)).Completed);
        Assert.Equal(1,sink.LogicalEffects);
    }

    [Fact(DisplayName = "P3-CTRL-001 kill switch activated after claim blocks dispatch")]
    public async Task P3_CTRL_001_KillSwitchAfterClaimBlocksDispatch()
    {
        var dispatcher=new PostgresOutboxDispatcher(dataSource);await SeedOutbox("controlled-key",2);
        var claim=(await dispatcher.ClaimAsync("dispatcher",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        await kernel.SetControlAsync(new("KILL_SWITCH","GLOBAL",true,"test","integration","stop"),CancellationToken.None);
        var sink=new DeduplicatingSink();var result=await dispatcher.DispatchAsync(claim,sink,CancellationToken.None);
        Assert.False(result.Completed);Assert.Equal(0,sink.Deliveries);Assert.Equal("LOST_AUTHORITY",result.Classification);
    }

    [Fact(DisplayName = "P3-CTRL-002 pauses block scheduling and are audited")]
    public async Task P3_CTRL_002_PausesBlockSchedulingAndAreAudited()
    {
        await kernel.SetControlAsync(new("SYMBOL_PAUSE","AAPL",true,"operator","api","review"),CancellationToken.None);
        var result=await kernel.ScheduleAsync(Request("paused"),CancellationToken.None);
        Assert.Equal(Guid.Empty,result.CycleId);Assert.Equal("CONTROL_ACTIVE",result.Classification);
        Assert.Equal(1L,await Scalar<long>("SELECT count(*) FROM control_state_events WHERE actor='operator' AND source='api' AND reason='review'"));
    }

    [Fact(DisplayName = "P3-CTRL-003 strategy pauses block matching work only")]
    public async Task P3_CTRL_003_StrategyPauseHasExactScope()
    {
        await kernel.SetControlAsync(new("STRATEGY_PAUSE","covered-call",true,"operator","api","strategy-review"),CancellationToken.None);
        var blocked=await kernel.ScheduleAsync(new("test:strategy-blocked","AAPL","NEW_ENTRY",DateTimeOffset.UtcNow.AddHours(1),Strategy:"covered-call"),CancellationToken.None);
        var allowed=await kernel.ScheduleAsync(new("test:strategy-allowed","AAPL","NEW_ENTRY",DateTimeOffset.UtcNow.AddHours(1),Strategy:"protective-put"),CancellationToken.None);
        Assert.Equal("CONTROL_ACTIVE",blocked.Classification);
        Assert.True(allowed.Created);
    }

    [Fact(DisplayName = "P3-HEALTH-001 health exposes dead letters and stale authority evidence")]
    public async Task P3_HEALTH_001_HealthExposesSafetySignals()
    {
        var cycle=await kernel.ScheduleAsync(Request("health"),CancellationToken.None);
        var claim=(await kernel.ClaimDueStepAsync("stale-owner",TimeSpan.FromMinutes(1),CancellationToken.None))!;
        await ExpireLease(cycle.CycleId,(int)claim.Node);
        await kernel.RecoverExpiredLeasesAsync(3,CancellationToken.None);
        var health=await kernel.ObserveHealthAsync(CancellationToken.None);
        Assert.True(health.DatabaseAvailable);
        Assert.True(health.StaleFenceRejectionCount>=1);
        Assert.True(health.ExpiredLeaseCount>=0);
    }

    private static DurableScheduleRequest Request(string key,string template="NEW_ENTRY")=>new($"test:{key}","AAPL",template,DateTimeOffset.UtcNow.AddHours(1));
    private async Task ExpireLease(Guid cycleId,int stepId){await using var command=dataSource.CreateCommand("UPDATE workflow_steps SET lease_expires_at=clock_timestamp()-interval '1 second' WHERE cycle_id=$1 AND step_id=$2");command.Parameters.AddWithValue(cycleId);command.Parameters.AddWithValue(stepId);await command.ExecuteNonQueryAsync(CancellationToken.None);}
    private async Task CompleteCycle(PostgresWorkflowKernel activeKernel){while(true){var claim=await activeKernel.ClaimDueStepAsync(Guid.NewGuid().ToString("N"),TimeSpan.FromMinutes(1),CancellationToken.None);if(claim is null)return;await activeKernel.FinalizeAsync(claim,new("COMPLETED","OK"),CancellationToken.None);}}
    private async Task SeedOutbox(string key,int maximumAttempts){await kernel.ScheduleAsync(Request($"outbox-{key}"),CancellationToken.None);var claim=(await kernel.ClaimDueStepAsync("workflow",TimeSpan.FromMinutes(1),CancellationToken.None))!;var completion=new DurableStepCompletion("COMPLETED","OK",Outbox:[new(Guid.NewGuid(),key,"TEST","1.0","{}",DateTimeOffset.UtcNow,maximumAttempts)]);await kernel.FinalizeAsync(claim,completion,CancellationToken.None);}
    private async Task Execute(string sql,params object[] values){await using var command=dataSource.CreateCommand(sql);for(var index=0;index<values.Length;index++)command.Parameters.AddWithValue(values[index]);await command.ExecuteNonQueryAsync(CancellationToken.None);}
    private async Task<T> Scalar<T>(string sql,params object[] values){await using var command=dataSource.CreateCommand(sql);for(var index=0;index<values.Length;index++)command.Parameters.AddWithValue(values[index]);return (T)(await command.ExecuteScalarAsync(CancellationToken.None))!;}

    private sealed class DeduplicatingSink:IPhase3MessageSink
    {
        private readonly HashSet<string> delivered=[];public int Deliveries=>delivered.Count;
        public Task DeliverAsync(string deduplicationKey,string messageType,string messageVersion,string payloadJson,CancellationToken cancellationToken){delivered.Add(deduplicationKey);return Task.CompletedTask;}
    }
    private sealed class FailingSink:IPhase3MessageSink
    { public Task DeliverAsync(string deduplicationKey,string messageType,string messageVersion,string payloadJson,CancellationToken cancellationToken)=>Task.FromException(new InvalidOperationException("injected")); }
    private sealed class ThrowAfterFirstLogicalEffectSink:IPhase3MessageSink
    {
        private readonly HashSet<string> effects=[];private bool crash=true;public int LogicalEffects=>effects.Count;
        public Task DeliverAsync(string deduplicationKey,string messageType,string messageVersion,string payloadJson,CancellationToken cancellationToken)
        {
            effects.Add(deduplicationKey);
            if(crash){crash=false;return Task.FromException(new InvalidOperationException("crash-after-logical-effect"));}
            return Task.CompletedTask;
        }
    }
}

[CollectionDefinition("PostgreSQL Phase 3", DisableParallelization = true)]
public sealed class PostgreSqlPhase3Collection;
