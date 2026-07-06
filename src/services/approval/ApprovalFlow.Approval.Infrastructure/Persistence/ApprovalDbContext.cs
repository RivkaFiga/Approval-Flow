using ApprovalFlow.Approval.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Approval.Infrastructure.Persistence;

/// <summary>
/// EF Core context owning Approval's durable business/audit data (§11): the workflow instance record and
/// the pending-approvals projection (§9.1). The workflow runtime state itself lives in the Dapr state
/// store; this context is the relational audit + queryable read model.
/// </summary>
public sealed class ApprovalDbContext : DbContext
{
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<PendingApproval> PendingApprovals => Set<PendingApproval>();

    public ApprovalDbContext(DbContextOptions<ApprovalDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("approval");

        modelBuilder.Entity<WorkflowInstance>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.TrackingId).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.TrackingId).IsUnique();

            e.Property(x => x.CorrelationId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Department).HasMaxLength(200).IsRequired();
            e.Property(x => x.AmountUsd).HasPrecision(18, 2);
            e.Property(x => x.CitedRulesJson).IsRequired();
            e.Property(x => x.FraudReason).HasMaxLength(1000);
        });

        modelBuilder.Entity<PendingApproval>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.TrackingId).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.TrackingId).IsUnique();

            e.Property(x => x.CorrelationId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Department).HasMaxLength(200).IsRequired();
            e.Property(x => x.AmountUsd).HasPrecision(18, 2);
            e.Property(x => x.CitedRulesJson).IsRequired();
        });
    }
}
