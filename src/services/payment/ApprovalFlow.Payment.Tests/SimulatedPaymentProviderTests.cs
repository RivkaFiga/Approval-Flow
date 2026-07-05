using ApprovalFlow.Payment.Domain.Values;
using ApprovalFlow.Payment.Infrastructure.Configuration;
using ApprovalFlow.Payment.Infrastructure.Providers;
using Microsoft.Extensions.Options;
using Xunit;

namespace ApprovalFlow.Payment.Tests;

public class SimulatedPaymentProviderTests
{
    private static SimulatedPaymentProvider Provider(SimulatedPaymentProviderOptions options)
        => new(Options.Create(options));

    private static ChargeCommand Command(string paymentId = "PAY-1", string trackingId = "TRK-1")
        => new(paymentId, trackingId, "corr-1", "engineering-2026Q2", 199.99m);

    [Fact]
    public async Task Succeeds_by_default_and_synthesizes_provider_reference()
    {
        var sut = Provider(new SimulatedPaymentProviderOptions());

        var result = await sut.ChargeAsync(Command("PAY-42"));

        Assert.Equal(PaymentProviderOutcome.Succeeded, result.Outcome);
        Assert.Equal("SIM-PAY-42", result.ProviderReference);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task Forces_failure_when_paymentId_is_on_fail_list()
    {
        var sut = Provider(new SimulatedPaymentProviderOptions
        {
            FailPaymentIds = new List<string> { "PAY-BAD" },
            FailureReason = "forced by test"
        });

        var result = await sut.ChargeAsync(Command("PAY-BAD"));

        Assert.Equal(PaymentProviderOutcome.Failed, result.Outcome);
        Assert.Null(result.ProviderReference);
        Assert.Equal("forced by test", result.Reason);
    }

    [Fact]
    public async Task Forces_failure_when_trackingId_is_on_fail_list()
    {
        var sut = Provider(new SimulatedPaymentProviderOptions
        {
            FailTrackingIds = new List<string> { "TRK-INV-1012" }
        });

        var result = await sut.ChargeAsync(Command(trackingId: "TRK-INV-1012"));

        Assert.Equal(PaymentProviderOutcome.Failed, result.Outcome);
    }

    [Fact]
    public async Task Does_not_fail_on_similar_but_non_matching_id()
    {
        var sut = Provider(new SimulatedPaymentProviderOptions
        {
            FailPaymentIds = new List<string> { "PAY-1" }
        });

        var result = await sut.ChargeAsync(Command("PAY-10"));

        Assert.Equal(PaymentProviderOutcome.Succeeded, result.Outcome);
    }
}
