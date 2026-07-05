using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Notification.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public DbSet<SubmissionStatus> SubmissionStatuses => Set<SubmissionStatus>();

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notification");

        modelBuilder.Entity<SubmissionStatus>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.TrackingId).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.TrackingId).IsUnique();

            e.Property(x => x.CorrelationId).HasMaxLength(50).IsRequired();
            e.Property(x => x.AmountUsd).HasPrecision(18, 2);
            e.Property(x => x.Reason).HasMaxLength(2000);
        });
    }
}
