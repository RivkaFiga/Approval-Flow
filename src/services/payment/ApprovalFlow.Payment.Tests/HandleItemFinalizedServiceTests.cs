using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Application.Services;
using ApprovalFlow.Payment.Domain.Entities;
using ApprovalFlow.Payment.Domain.Values;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Payment.Tests;

/// <summary>
/// Tests the ItemFinalized → Payment → PaymentCompleted saga orchestration (§8). Uses real Reserve /
/// Execute / Release services against substituted ports so the three-step saga is exercised end-to-end
/// with mocked infrastructure only.
/// </summary>
public class HandleItemFinalizedServiceTests
{
    private static readonly DateTimeOffset TestNow = new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly IPaymentRecordRepository _records = Substitute.For<IPaymentRecordRepository>();
    private readonly IBudgetStore _budgets = Substitute.For<IBudgetStore>();
    private readonly IPaymentIdempotencyStore _idempotency = Substitute.For<IPaymentIdempotencyStore>();
    private readonly IPaymentLedgerRepository _ledger = Substitute.For<IPaymentLedgerRepository>();
    private readonly IPaymentProvider _provider = Substitute.For<IPaymentProvider>();
    private readonly IPaymentEventPublisher _publisher = Substitute.For<IPaymentEventPublisher>();
    private readonly TimeProvider _clock = new FakeTimeProvider(TestNow);

    private readonly HandleItemFinalizedService _sut;

    public HandleItemFinalizedServiceTests()
    {
        var reserve = new ReserveBudgetService(_budgets, _idempotency, NullLogger<ReserveBudgetService>.Instance);
        var execute = new ExecutePaymentService(_idempotency, _ledger, _provider, _clock, NullLogger<ExecutePaymentService>.Instance);
        var release = new ReleaseReservationService(_budgets, _idempotency, NullLogger<ReleaseReservationService>.Instance);

        _sut = new HandleItemFinalizedService(
            _records, reserve, execute, release, _publisher, _clock,
            NullLogger<HandleItemFinalizedService>.Instance);
    }

    private static ItemFinalizedV1 ApprovedEvent(decimal amount = 200m, string trackingId = "TRK-1") => new()
    {
        CorrelationId = "corr-1",
        OccurredAt = TestNow,
        TrackingId = trackingId,
        FinalStatus = LifecycleStatus.Paid,
        Reason = "Auto-approved.",
        PaymentOutcome = PaymentOutcome.Paid,
        ApprovalPath = ApprovalPath.Auto,
        AmountUsd = amount,
        Department = "engineering-2026Q2"
    };

    private static BudgetSnapshot Snapshot(decimal remaining, string etag = "etag-1")
        => new(DepartmentBudget.Rehydrate("engineering-2026Q2", remaining), etag);

    [Fact]
    public async Task Skips_saga_when_finalized_has_no_payment_leg()
    {
        var rejected = ApprovedEvent() with { PaymentOutcome = null, FinalStatus = LifecycleStatus.Rejected };

        await _sut.HandleAsync(rejected);

        await _records.DidNotReceive().AddAsync(Arg.Any<PaymentRecord>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishPaymentCompletedAsync(Arg.Any<PaymentCompletedV1>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reserves_pays_and_publishes_paid_on_provider_success()
    {
        _records.GetByTrackingIdAsync("TRK-1").Returns((PaymentRecord?)null);
        _idempotency.GetReserveResultAsync("pay-TRK-1").Returns((ReserveBudgetResult?)null);
        _budgets.LoadAsync("engineering-2026Q2").Returns(Snapshot(1000m));
        _budgets.TryWriteAsync(Arg.Any<DepartmentBudget>(), "etag-1").Returns(true);
        _idempotency.GetExecuteResultAsync("pay-TRK-1").Returns((ExecutePaymentResult?)null);
        // The reserve step saves a ReserveBudgetResult; the execute step needs to see it as active.
        _ = _idempotency.SaveReserveResultAsync("pay-TRK-1", Arg.Do<ReserveBudgetResult>(r =>
            _idempotency.GetReserveResultAsync("pay-TRK-1").Returns(r)));
        _provider.ChargeAsync(Arg.Any<ApprovalFlow.Payment.Domain.Values.ChargeCommand>(), Arg.Any<CancellationToken>())
            .Returns(ApprovalFlow.Payment.Domain.Values.ChargeResult.Success("SIM-1"));
        _ledger.TryAppendAsync(Arg.Any<PaymentLedgerEntry>()).Returns(true);

        // Snapshot the record at AddAsync time — later saga steps mutate Status in place, so a Received(...)
        // predicate would otherwise see the terminal state instead of the initial Reserved.
        PaymentRecordStatus? initialStatus = null;
        string? initialPaymentId = null;
        _ = _records.AddAsync(Arg.Do<PaymentRecord>(r =>
        {
            initialStatus = r.Status;
            initialPaymentId = r.PaymentId;
        }), Arg.Any<CancellationToken>());

        PaymentCompletedV1? published = null;
        _ = _publisher.PublishPaymentCompletedAsync(
            Arg.Do<PaymentCompletedV1>(e => published = e), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(ApprovedEvent(200m));

        Assert.Equal("pay-TRK-1", initialPaymentId);
        Assert.Equal(PaymentRecordStatus.Reserved, initialStatus);
        Assert.NotNull(published);
        Assert.Equal(PaymentOutcome.Paid, published!.Outcome);
        Assert.Equal("pay-TRK-1", published.PaymentId);
        Assert.Equal(200m, published.AmountUsd);
        Assert.False(string.IsNullOrEmpty(published.LedgerEntryId));
    }

    [Fact]
    public async Task Compensates_and_publishes_failure_when_provider_declines()
    {
        _records.GetByTrackingIdAsync("TRK-1").Returns((PaymentRecord?)null);
        _idempotency.GetReserveResultAsync("pay-TRK-1").Returns((ReserveBudgetResult?)null);
        _budgets.LoadAsync("engineering-2026Q2").Returns(Snapshot(1000m));
        _budgets.TryWriteAsync(Arg.Any<DepartmentBudget>(), "etag-1").Returns(true);
        _idempotency.GetExecuteResultAsync("pay-TRK-1").Returns((ExecutePaymentResult?)null);
        _ = _idempotency.SaveReserveResultAsync("pay-TRK-1", Arg.Do<ReserveBudgetResult>(r =>
            _idempotency.GetReserveResultAsync("pay-TRK-1").Returns(r)));
        _provider.ChargeAsync(Arg.Any<ApprovalFlow.Payment.Domain.Values.ChargeCommand>(), Arg.Any<CancellationToken>())
            .Returns(ApprovalFlow.Payment.Domain.Values.ChargeResult.Failure("simulated failure"));

        PaymentCompletedV1? published = null;
        _ = _publisher.PublishPaymentCompletedAsync(
            Arg.Do<PaymentCompletedV1>(e => published = e), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(ApprovedEvent(200m));

        // Two writes: reserve (draw) + release (restore). The release brings the balance back to 1000.
        await _budgets.Received(2).TryWriteAsync(Arg.Any<DepartmentBudget>(), Arg.Any<string>());
        Assert.NotNull(published);
        Assert.Equal(PaymentOutcome.PaymentFailed, published!.Outcome);
        Assert.Equal("simulated failure", published.Reason);
        Assert.Null(published.LedgerEntryId);
    }

    [Fact]
    public async Task Publishes_insufficient_budget_and_skips_execute_when_reserve_refuses()
    {
        _records.GetByTrackingIdAsync("TRK-1").Returns((PaymentRecord?)null);
        _idempotency.GetReserveResultAsync("pay-TRK-1").Returns((ReserveBudgetResult?)null);
        _budgets.LoadAsync("engineering-2026Q2").Returns(Snapshot(50m));

        PaymentCompletedV1? published = null;
        _ = _publisher.PublishPaymentCompletedAsync(
            Arg.Do<PaymentCompletedV1>(e => published = e), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(ApprovedEvent(200m));

        await _provider.DidNotReceive().ChargeAsync(Arg.Any<ApprovalFlow.Payment.Domain.Values.ChargeCommand>(), Arg.Any<CancellationToken>());
        await _ledger.DidNotReceive().TryAppendAsync(Arg.Any<PaymentLedgerEntry>(), Arg.Any<CancellationToken>());
        Assert.NotNull(published);
        Assert.Equal(PaymentOutcome.InsufficientBudget, published!.Outcome);
    }

    [Fact]
    public async Task Replays_prior_record_on_redelivery_without_touching_provider()
    {
        var prior = PaymentRecord.StartReserved("pay-TRK-1", "TRK-1", "corr-1", "engineering-2026Q2", 200m, TestNow);
        prior.MarkPaid(Guid.NewGuid().ToString(), TestNow);
        _records.GetByTrackingIdAsync("TRK-1").Returns(prior);

        PaymentCompletedV1? published = null;
        _ = _publisher.PublishPaymentCompletedAsync(
            Arg.Do<PaymentCompletedV1>(e => published = e), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(ApprovedEvent(200m));

        await _records.DidNotReceive().AddAsync(Arg.Any<PaymentRecord>(), Arg.Any<CancellationToken>());
        await _budgets.DidNotReceive().TryWriteAsync(Arg.Any<DepartmentBudget>(), Arg.Any<string>());
        await _provider.DidNotReceive().ChargeAsync(Arg.Any<ApprovalFlow.Payment.Domain.Values.ChargeCommand>(), Arg.Any<CancellationToken>());
        Assert.NotNull(published);
        Assert.Equal(PaymentOutcome.Paid, published!.Outcome);
    }

    [Fact]
    public async Task Throws_when_department_missing_on_approved_event()
    {
        var bad = ApprovedEvent() with { Department = "" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(bad));
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
