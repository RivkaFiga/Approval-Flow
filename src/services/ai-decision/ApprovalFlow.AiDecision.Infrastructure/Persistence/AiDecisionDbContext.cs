using ApprovalFlow.AiDecision.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.AiDecision.Infrastructure.Persistence;

public sealed class AiDecisionDbContext : DbContext
{
    public DbSet<Decision> Decisions => Set<Decision>();

    public AiDecisionDbContext(DbContextOptions<AiDecisionDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ai_decision");

        modelBuilder.Entity<Decision>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.TrackingId).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.TrackingId).IsUnique();

            e.Property(x => x.CorrelationId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Department).HasMaxLength(200).IsRequired();
            e.Property(x => x.AmountUsd).HasPrecision(18, 2);
            e.Property(x => x.CitedRulesJson).IsRequired();
            e.Property(x => x.FraudReason).HasMaxLength(1000);
            e.Property(x => x.PolicyVersion).HasMaxLength(50).IsRequired();
        });
    }
}
