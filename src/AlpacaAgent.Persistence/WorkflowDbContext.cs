using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlpacaAgent.Persistence;

public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowCycleRow> WorkflowCycles => Set<WorkflowCycleRow>();
    public DbSet<WorkflowStepRow> WorkflowSteps => Set<WorkflowStepRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowCycleRow>(entity =>
        {
            entity.ToTable("workflow_cycles"); entity.HasKey(x => x.CycleId);
            entity.Property(x => x.CycleId).HasColumnName("cycle_id"); entity.Property(x => x.ScheduleKey).HasColumnName("schedule_key");
            entity.Property(x => x.Template).HasColumnName("template"); entity.Property(x => x.Symbol).HasColumnName("symbol");
            entity.Property(x => x.Status).HasColumnName("status"); entity.Property(x => x.Deadline).HasColumnName("deadline");
            entity.HasIndex(x => x.ScheduleKey).IsUnique();
        });
        modelBuilder.Entity<WorkflowStepRow>(entity =>
        {
            entity.ToTable("workflow_steps"); entity.HasKey(x => new { x.CycleId, x.StepId });
            entity.Property(x => x.CycleId).HasColumnName("cycle_id"); entity.Property(x => x.StepId).HasColumnName("step_id");
            entity.Property(x => x.Status).HasColumnName("status"); entity.Property(x => x.RoutingReason).HasColumnName("routing_reason");
            entity.Property(x => x.DueAt).HasColumnName("due_at"); entity.Property(x => x.LeaseOwner).HasColumnName("lease_owner");
            entity.Property(x => x.LeaseExpiresAt).HasColumnName("lease_expires_at"); entity.Property(x => x.FencingToken).HasColumnName("fencing_token");
            entity.Property(x => x.AttemptNumber).HasColumnName("attempt_number");
        });
    }
}

public sealed class WorkflowDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseNpgsql("Host=localhost;Database=alpaca_agent_design;Username=postgres;Password=postgres")
            .Options;
        return new(options);
    }
}

public sealed class WorkflowCycleRow
{
    public Guid CycleId { get; set; }
    public string ScheduleKey { get; set; } = "";
    public string Template { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Status { get; set; } = "RUNNING";
    public DateTimeOffset Deadline { get; set; }
}

public sealed class WorkflowStepRow
{
    public Guid CycleId { get; set; }
    public int StepId { get; set; }
    public string Status { get; set; } = "PENDING";
    public string RoutingReason { get; set; } = "";
    public DateTimeOffset? DueAt { get; set; }
    public string? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public long FencingToken { get; set; }
    public int AttemptNumber { get; set; }
}
