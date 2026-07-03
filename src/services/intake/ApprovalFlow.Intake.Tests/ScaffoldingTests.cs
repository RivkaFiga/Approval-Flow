using ApprovalFlow.Intake.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApprovalFlow.Intake.Tests;

public class SubmittedInvoiceRepositoryTests
{
    private static IntakeDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<IntakeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IntakeDbContext(options);
    }

    [Fact]
    public async Task ExistsByDedupKey_returns_false_when_empty()
    {
        using var db = CreateInMemoryDb();
        var repo = new SubmittedInvoiceRepository(db);

        var exists = await repo.ExistsByDedupKeyAsync("nonexistent");

        Assert.False(exists);
    }

    [Fact]
    public async Task Add_and_find_by_dedup_key()
    {
        using var db = CreateInMemoryDb();
        var repo = new SubmittedInvoiceRepository(db);

        var entity = Domain.Entities.SubmittedInvoice.Create(
            "TRK-20260703-ABCD1234", "corr-1", "INV-001", "Acme", true,
            "jane@test.com", "Engineering", 1, "USD", 10m, 100m,
            true, null, new DateOnly(2026, 7, 1), null, "[]", "dedup123");

        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        var exists = await repo.ExistsByDedupKeyAsync("dedup123");
        Assert.True(exists);
    }
}
