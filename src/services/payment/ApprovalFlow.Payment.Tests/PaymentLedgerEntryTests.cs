using ApprovalFlow.Payment.Domain.Entities;
using Xunit;

namespace ApprovalFlow.Payment.Tests;

public class PaymentLedgerEntryTests
{
    private static PaymentLedgerEntry Valid() => PaymentLedgerEntry.Create(
        paymentId: "PAY-1",
        trackingId: "TRK-1",
        correlationId: "corr-1",
        department: "engineering-2026Q2",
        amountUsd: 199.99m,
        providerReference: "SIM-PAY-1",
        createdAt: new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_populates_all_fields_and_generates_id()
    {
        var entry = Valid();

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal("PAY-1", entry.PaymentId);
        Assert.Equal("TRK-1", entry.TrackingId);
        Assert.Equal("corr-1", entry.CorrelationId);
        Assert.Equal("engineering-2026Q2", entry.Department);
        Assert.Equal(199.99m, entry.AmountUsd);
        Assert.Equal("SIM-PAY-1", entry.ProviderReference);
        Assert.Equal(new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero), entry.CreatedAt);
    }

    [Theory]
    [InlineData("", "TRK", "dept", "ref")]
    [InlineData("PAY", "", "dept", "ref")]
    [InlineData("PAY", "TRK", "", "ref")]
    [InlineData("PAY", "TRK", "dept", "")]
    public void Create_rejects_missing_required_fields(string paymentId, string trackingId, string department, string providerReference)
    {
        Assert.Throws<ArgumentException>(() => PaymentLedgerEntry.Create(
            paymentId, trackingId, "corr", department, 100m, providerReference, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_rejects_non_positive_amount(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PaymentLedgerEntry.Create(
            "PAY", "TRK", "corr", "dept", amount, "ref", DateTimeOffset.UtcNow));
    }
}
