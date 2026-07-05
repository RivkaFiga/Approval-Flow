using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Application.Services;
using ApprovalFlow.Payment.Domain.Entities;
using ApprovalFlow.Payment.Domain.Values;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Payment.Tests;

public class ExecutePaymentServiceTests
{
    private static readonly DateTimeOffset TestNow = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly IPaymentIdempotencyStore _idempotency = Substitute.For<IPaymentIdempotencyStore>();
    private readonly IPaymentLedgerRepository _ledger = Substitute.For<IPaymentLedgerRepository>();
    private readonly IPaymentProvider _provider = Substitute.For<IPaymentProvider>();
    private readonly TimeProvider _clock;
    private readonly ExecutePaymentService _sut;

    public ExecutePaymentServiceTests()
    {
        _clock = new FakeTimeProvider(TestNow);
        _sut = new ExecutePaymentService(
            _idempotency, _ledger, _provider, _clock, NullLogger<ExecutePaymentService>.Instance);
    }

    private static ExecutePaymentRequest Request(string paymentId = "PAY-1") => new()
    {
        CorrelationId = "corr-1",
        TrackingId = "TRK-1",
        Department = "engineering-2026Q2",
        AmountUsd = 199.99m,
        PaymentId = paymentId
    };

    private static ReserveBudgetResult ReservedResult() =>
        new() { Reserved = true, RemainingBudget = 800.01m };

    private void SetupProviderSuccess(string providerReference = "SIM-PAY-1")
        => _provider.ChargeAsync(Arg.Any<ChargeCommand>(), Arg.Any<CancellationToken>())
            .Returns(ChargeResult.Success(providerReference));

    private void SetupProviderFailure(string reason)
        => _provider.ChargeAsync(Arg.Any<ChargeCommand>(), Arg.Any<CancellationToken>())
            .Returns(ChargeResult.Failure(reason));

    [Fact]
    public async Task Pays_appends_ledger_and_persists_idempotency_on_provider_success()
    {
        _idempotency.GetExecuteResultAsync("PAY-1").Returns((ExecutePaymentResult?)null);
        _idempotency.GetReserveResultAsync("PAY-1").Returns(ReservedResult());
        SetupProviderSuccess("SIM-PAY-1");
        _ledger.TryAppendAsync(Arg.Any<PaymentLedgerEntry>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.HandleAsync(Request());

        Assert.Equal(PaymentOutcome.Paid, result.Outcome);
        Assert.False(string.IsNullOrEmpty(result.LedgerEntryId));
        Assert.Equal("PAY-1", result.PaymentId);
        await _ledger.Received(1).TryAppendAsync(
            Arg.Is<PaymentLedgerEntry>(e => e.PaymentId == "PAY-1" && e.ProviderReference == "SIM-PAY-1"),
            Arg.Any<CancellationToken>());
        await _idempotency.Received(1).SaveExecuteResultAsync(
            "PAY-1",
            Arg.Is<ExecutePaymentResult>(r => r.Outcome == PaymentOutcome.Paid));
    }

    [Fact]
    public async Task Refuses_when_no_reservation_exists()
    {
        _idempotency.GetExecuteResultAsync("PAY-1").Returns((ExecutePaymentResult?)null);
        _idempotency.GetReserveResultAsync("PAY-1").Returns((ReserveBudgetResult?)null);

        var result = await _sut.HandleAsync(Request());

        Assert.Equal(PaymentOutcome.PaymentFailed, result.Outcome);
        Assert.Null(result.LedgerEntryId);
        await _provider.DidNotReceive().ChargeAsync(Arg.Any<ChargeCommand>(), Arg.Any<CancellationToken>());
        await _ledger.DidNotReceive().TryAppendAsync(Arg.Any<PaymentLedgerEntry>(), Arg.Any<CancellationToken>());
        await _idempotency.Received(1).SaveExecuteResultAsync("PAY-1", Arg.Is<ExecutePaymentResult>(r => r.Outcome == PaymentOutcome.PaymentFailed));
    }

    [Fact]
    public async Task Refuses_when_reservation_was_refused()
    {
        _idempotency.GetExecuteResultAsync("PAY-1").Returns((ExecutePaymentResult?)null);
        _idempotency.GetReserveResultAsync("PAY-1").Returns(new ReserveBudgetResult
        {
            Reserved = false,
            Outcome = PaymentOutcome.InsufficientBudget
        });

        var result = await _sut.HandleAsync(Request());

        Assert.Equal(PaymentOutcome.PaymentFailed, result.Outcome);
        await _provider.DidNotReceive().ChargeAsync(Arg.Any<ChargeCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Records_failure_on_provider_decline_and_does_not_append_ledger()
    {
        _idempotency.GetExecuteResultAsync("PAY-1").Returns((ExecutePaymentResult?)null);
        _idempotency.GetReserveResultAsync("PAY-1").Returns(ReservedResult());
        SetupProviderFailure("simulated failure");

        var result = await _sut.HandleAsync(Request());

        Assert.Equal(PaymentOutcome.PaymentFailed, result.Outcome);
        Assert.Null(result.LedgerEntryId);
        Assert.Equal("simulated failure", result.Reason);
        await _ledger.DidNotReceive().TryAppendAsync(Arg.Any<PaymentLedgerEntry>(), Arg.Any<CancellationToken>());
        await _idempotency.Received(1).SaveExecuteResultAsync("PAY-1", Arg.Is<ExecutePaymentResult>(r => r.Outcome == PaymentOutcome.PaymentFailed));
    }

    [Fact]
    public async Task Replays_prior_execute_result_and_skips_provider()
    {
        var prior = new ExecutePaymentResult
        {
            Outcome = PaymentOutcome.Paid,
            PaymentId = "PAY-1",
            LedgerEntryId = Guid.NewGuid().ToString()
        };
        _idempotency.GetExecuteResultAsync("PAY-1").Returns(prior);

        var result = await _sut.HandleAsync(Request());

        Assert.Same(prior, result);
        await _idempotency.DidNotReceive().GetReserveResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _provider.DidNotReceive().ChargeAsync(Arg.Any<ChargeCommand>(), Arg.Any<CancellationToken>());
        await _ledger.DidNotReceive().TryAppendAsync(Arg.Any<PaymentLedgerEntry>(), Arg.Any<CancellationToken>());
        await _idempotency.DidNotReceive().SaveExecuteResultAsync(Arg.Any<string>(), Arg.Any<ExecutePaymentResult>());
    }

    [Fact]
    public async Task Replays_existing_ledger_row_on_unique_conflict()
    {
        _idempotency.GetExecuteResultAsync("PAY-1").Returns((ExecutePaymentResult?)null);
        _idempotency.GetReserveResultAsync("PAY-1").Returns(ReservedResult());
        SetupProviderSuccess("SIM-PAY-1");
        _ledger.TryAppendAsync(Arg.Any<PaymentLedgerEntry>(), Arg.Any<CancellationToken>()).Returns(false);

        var existing = PaymentLedgerEntry.Create(
            "PAY-1", "TRK-1", "corr-1", "engineering-2026Q2", 199.99m, "SIM-PAY-1", TestNow);
        _ledger.GetByPaymentIdAsync("PAY-1", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.HandleAsync(Request());

        Assert.Equal(PaymentOutcome.Paid, result.Outcome);
        Assert.Equal(existing.Id.ToString(), result.LedgerEntryId);
    }

    [Fact]
    public async Task Throws_when_ledger_says_conflict_but_no_row_exists()
    {
        _idempotency.GetExecuteResultAsync("PAY-1").Returns((ExecutePaymentResult?)null);
        _idempotency.GetReserveResultAsync("PAY-1").Returns(ReservedResult());
        SetupProviderSuccess("SIM-PAY-1");
        _ledger.TryAppendAsync(Arg.Any<PaymentLedgerEntry>(), Arg.Any<CancellationToken>()).Returns(false);
        _ledger.GetByPaymentIdAsync("PAY-1", Arg.Any<CancellationToken>()).Returns((PaymentLedgerEntry?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(Request()));
    }

    [Theory]
    [InlineData("", "TRK", "dept", 100)]
    [InlineData("PAY", "", "dept", 100)]
    [InlineData("PAY", "TRK", "", 100)]
    public async Task Rejects_invalid_request_arguments(string paymentId, string trackingId, string department, decimal amount)
    {
        var request = new ExecutePaymentRequest
        {
            CorrelationId = "corr",
            TrackingId = trackingId,
            Department = department,
            AmountUsd = amount,
            PaymentId = paymentId
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.HandleAsync(request));
    }

    [Fact]
    public async Task Rejects_non_positive_amount()
    {
        var request = new ExecutePaymentRequest
        {
            CorrelationId = "corr",
            TrackingId = "TRK",
            Department = "dept",
            AmountUsd = 0m,
            PaymentId = "PAY"
        };
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.HandleAsync(request));
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
