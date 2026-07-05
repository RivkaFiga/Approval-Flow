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

public class ReserveBudgetServiceTests
{
    private readonly IBudgetStore _store = Substitute.For<IBudgetStore>();
    private readonly IPaymentIdempotencyStore _idempotency = Substitute.For<IPaymentIdempotencyStore>();
    private readonly ReserveBudgetService _sut;

    public ReserveBudgetServiceTests()
    {
        _sut = new ReserveBudgetService(_store, _idempotency, NullLogger<ReserveBudgetService>.Instance);
    }

    private static ReserveBudgetRequest Request(decimal amount, string paymentId = "PAY-1") => new()
    {
        CorrelationId = "corr-1",
        TrackingId = "TRK-1",
        Department = "marketing-2026Q2",
        AmountUsd = amount,
        PaymentId = paymentId
    };

    private static BudgetSnapshot Snapshot(decimal remaining, string etag = "etag-1")
        => new(DepartmentBudget.Rehydrate("marketing-2026Q2", remaining), etag);

    [Fact]
    public async Task Reserves_and_persists_idempotency_when_budget_sufficient()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns((ReserveBudgetResult?)null);
        _store.LoadAsync("marketing-2026Q2").Returns(Snapshot(1000m));
        _store.TryWriteAsync(Arg.Any<DepartmentBudget>(), "etag-1").Returns(true);

        var result = await _sut.HandleAsync(Request(400m));

        Assert.True(result.Reserved);
        Assert.Equal(600m, result.RemainingBudget);
        Assert.Null(result.Outcome);
        await _idempotency.Received(1).SaveReserveResultAsync("PAY-1", Arg.Is<ReserveBudgetResult>(r => r.Reserved && r.RemainingBudget == 600m));
    }

    [Fact]
    public async Task Refuses_and_persists_idempotency_when_budget_insufficient()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns((ReserveBudgetResult?)null);
        _store.LoadAsync("marketing-2026Q2").Returns(Snapshot(100m));

        var result = await _sut.HandleAsync(Request(400m));

        Assert.False(result.Reserved);
        Assert.Equal(100m, result.RemainingBudget);
        Assert.Equal(PaymentOutcome.InsufficientBudget, result.Outcome);
        await _store.DidNotReceive().TryWriteAsync(Arg.Any<DepartmentBudget>(), Arg.Any<string>());
        await _idempotency.Received(1).SaveReserveResultAsync("PAY-1", Arg.Is<ReserveBudgetResult>(r => !r.Reserved));
    }

    [Fact]
    public async Task Retries_on_etag_conflict_then_succeeds()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns((ReserveBudgetResult?)null);
        _store.LoadAsync("marketing-2026Q2").Returns(
            Snapshot(1000m, "etag-a"),
            Snapshot(600m, "etag-b"));
        _store.TryWriteAsync(Arg.Any<DepartmentBudget>(), "etag-a").Returns(false);
        _store.TryWriteAsync(Arg.Any<DepartmentBudget>(), "etag-b").Returns(true);

        var result = await _sut.HandleAsync(Request(400m));

        Assert.True(result.Reserved);
        Assert.Equal(200m, result.RemainingBudget);
        await _store.Received(2).LoadAsync("marketing-2026Q2", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replays_prior_result_when_paymentId_already_processed()
    {
        var prior = new ReserveBudgetResult { Reserved = true, RemainingBudget = 600m };
        _idempotency.GetReserveResultAsync("PAY-1").Returns(prior);

        var result = await _sut.HandleAsync(Request(400m));

        Assert.Same(prior, result);
        await _store.DidNotReceive().LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().TryWriteAsync(Arg.Any<DepartmentBudget>(), Arg.Any<string>());
        await _idempotency.DidNotReceive().SaveReserveResultAsync(Arg.Any<string>(), Arg.Any<ReserveBudgetResult>());
    }

    [Fact]
    public async Task Refuses_when_department_has_no_budget_seeded()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns((ReserveBudgetResult?)null);
        _store.LoadAsync("marketing-2026Q2").Returns((BudgetSnapshot?)null);

        var result = await _sut.HandleAsync(Request(100m));

        Assert.False(result.Reserved);
        Assert.Equal(PaymentOutcome.InsufficientBudget, result.Outcome);
        Assert.Contains("marketing-2026Q2", result.Reason);
        await _idempotency.Received(1).SaveReserveResultAsync("PAY-1", Arg.Is<ReserveBudgetResult>(r => !r.Reserved));
    }

    [Theory]
    [InlineData("", "TRK", "PAY")]
    [InlineData("dept", "TRK", "")]
    public async Task Rejects_invalid_request_arguments(string department, string trackingId, string paymentId)
    {
        var request = new ReserveBudgetRequest
        {
            CorrelationId = "corr",
            TrackingId = trackingId,
            Department = department,
            AmountUsd = 100m,
            PaymentId = paymentId
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.HandleAsync(request));
    }

    [Fact]
    public async Task Rejects_non_positive_amount()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.HandleAsync(Request(0m)));
    }
}
