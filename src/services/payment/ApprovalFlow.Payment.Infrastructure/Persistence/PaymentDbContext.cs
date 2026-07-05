using ApprovalFlow.Payment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext : DbContext
{
    public DbSet<PaymentLedgerEntry> PaymentLedgerEntries => Set<PaymentLedgerEntry>();

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payment");

        modelBuilder.Entity<PaymentLedgerEntry>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.PaymentId).HasMaxLength(100).IsRequired();
            // Enforces §10's "exactly one payment per idempotency key" at the database layer — the
            // application-level idempotency store is the fast path, this is the concurrency safety net.
            e.HasIndex(x => x.PaymentId).IsUnique();

            e.Property(x => x.TrackingId).HasMaxLength(50).IsRequired();
            e.Property(x => x.CorrelationId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Department).HasMaxLength(200).IsRequired();
            e.Property(x => x.AmountUsd).HasPrecision(18, 2);
            e.Property(x => x.ProviderReference).HasMaxLength(200).IsRequired();
        });
    }
}
