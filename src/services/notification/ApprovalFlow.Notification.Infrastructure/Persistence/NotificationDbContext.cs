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

            // Optimistic-concurrency token. Each handler opens its own scoped DbContext, so two events
            // for the same TrackingId can load the row, mutate it, and race to SaveChanges. The domain's
            // IsNewer(occurredAt) guard compares to the *loaded* UpdatedAt, which does not reflect a
            // concurrent write, so without this token a later save could overwrite an earlier winner and
            // even regress UpdatedAt. Including UpdatedAt in the UPDATE WHERE clause turns a conflict into
            // DbUpdateConcurrencyException → Dapr redelivers → on retry the row's UpdatedAt is current and
            // IsNewer correctly no-ops the stale event.
            e.Property(x => x.UpdatedAt).IsConcurrencyToken();
        });
    }
}
