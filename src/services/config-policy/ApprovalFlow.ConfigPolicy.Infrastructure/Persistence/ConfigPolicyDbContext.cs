using ApprovalFlow.ConfigPolicy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.ConfigPolicy.Infrastructure.Persistence;

public sealed class ConfigPolicyDbContext : DbContext
{
    public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();
    public DbSet<FxRateEntry> FxRates => Set<FxRateEntry>();
    public DbSet<KnownVendorEntry> KnownVendors => Set<KnownVendorEntry>();

    public ConfigPolicyDbContext(DbContextOptions<ConfigPolicyDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("config_policy");

        modelBuilder.Entity<PolicyDocument>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Markdown).IsRequired();
            e.Property(p => p.AutonomyCeilingUsd).HasPrecision(18, 2);
            e.Property(p => p.BaseCurrency).HasMaxLength(3).IsRequired();
            e.Property(p => p.Version).IsConcurrencyToken();
            e.Property(p => p.RowVersion).IsRowVersion();

            e.HasMany(p => p.FxRates)
                .WithOne()
                .HasForeignKey(f => f.PolicyDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.KnownVendors)
                .WithOne()
                .HasForeignKey(v => v.PolicyDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => p.IsActive)
                .HasFilter("\"IsActive\" = true")
                .HasDatabaseName("IX_PolicyDocuments_Active");
        });

        modelBuilder.Entity<FxRateEntry>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(f => f.RateToBaseCurrency).HasPrecision(18, 6);
        });

        modelBuilder.Entity<KnownVendorEntry>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.VendorName).HasMaxLength(300).IsRequired();
        });
    }
}
