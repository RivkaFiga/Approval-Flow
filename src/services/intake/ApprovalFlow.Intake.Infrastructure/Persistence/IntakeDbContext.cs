using ApprovalFlow.Intake.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Intake.Infrastructure.Persistence;

public sealed class IntakeDbContext : DbContext
{
    public DbSet<SubmittedInvoice> SubmittedInvoices => Set<SubmittedInvoice>();

    public IntakeDbContext(DbContextOptions<IntakeDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("intake");

        modelBuilder.Entity<SubmittedInvoice>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.TrackingId).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.TrackingId).IsUnique();

            e.Property(x => x.CorrelationId).HasMaxLength(50).IsRequired();

            e.Property(x => x.InvoiceNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Vendor).HasMaxLength(300).IsRequired();
            e.Property(x => x.Submitter).HasMaxLength(200).IsRequired();
            e.Property(x => x.Department).HasMaxLength(200).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.LineItemsJson).IsRequired();

            e.Property(x => x.DedupKey).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.DedupKey).IsUnique();
        });
    }
}
