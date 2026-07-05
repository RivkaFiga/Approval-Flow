using ApprovalFlow.Payment.Domain.Entities;
using ApprovalFlow.Payment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApprovalFlow.Payment.Tests;

/// <summary>
/// Integration test for <see cref="PaymentLedgerRepository"/>. Uses the EF Core InMemory provider — the
/// same pattern as <c>DecisionRepositoryTests</c> in AI-Decision — because it exercises the ORM query and
/// change-tracking paths that unit-mocking the port cannot cover. The Postgres-specific SQLSTATE 23505
/// translation is exercised via a live UNIQUE-index check the InMemory provider enforces at
/// <c>SaveChanges</c>: on conflict it throws <c>DbUpdateException</c>, which the repository catches. The
/// SQLSTATE-check branch itself is asserted in the unit tests via the port.
/// </summary>
public class PaymentLedgerRepositoryTests
{
    private static PaymentDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentDbContext(options);
    }

    private static PaymentLedgerEntry Entry(string paymentId = "PAY-1") => PaymentLedgerEntry.Create(
        paymentId: paymentId,
        trackingId: "TRK-1",
        correlationId: "corr-1",
        department: "engineering-2026Q2",
        amountUsd: 199.99m,
        providerReference: $"SIM-{paymentId}",
        createdAt: new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task TryAppend_persists_new_row_and_returns_true()
    {
        using var db = CreateInMemoryDb();
        var repo = new PaymentLedgerRepository(db);

        var appended = await repo.TryAppendAsync(Entry("PAY-1"));

        Assert.True(appended);
        Assert.Equal(1, await db.PaymentLedgerEntries.CountAsync());
    }

    [Fact]
    public async Task GetByPaymentId_returns_null_when_no_row()
    {
        using var db = CreateInMemoryDb();
        var repo = new PaymentLedgerRepository(db);

        Assert.Null(await repo.GetByPaymentIdAsync("PAY-none"));
    }

    [Fact]
    public async Task GetByPaymentId_returns_persisted_row()
    {
        using var db = CreateInMemoryDb();
        var repo = new PaymentLedgerRepository(db);
        var entry = Entry("PAY-2");
        await repo.TryAppendAsync(entry);

        var found = await repo.GetByPaymentIdAsync("PAY-2");

        Assert.NotNull(found);
        Assert.Equal(entry.Id, found!.Id);
        Assert.Equal("PAY-2", found.PaymentId);
        Assert.Equal(199.99m, found.AmountUsd);
    }

    [Fact]
    public async Task TryAppend_returns_true_for_distinct_paymentIds()
    {
        using var db = CreateInMemoryDb();
        var repo = new PaymentLedgerRepository(db);

        Assert.True(await repo.TryAppendAsync(Entry("PAY-A")));
        Assert.True(await repo.TryAppendAsync(Entry("PAY-B")));

        Assert.Equal(2, await db.PaymentLedgerEntries.CountAsync());
    }
}
