using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Application.Services;
using ApprovalFlow.Payment.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Payment.Tests;

public class ReleaseReservationServiceTests
{
    private readonly IBudgetStore _store = Substitute.For<IBudgetStore>();
    private readonly IPaymentIdempotencyStore _idempotency = Substitute.For<IPaymentIdempotencyStore>();
    private readonly ReleaseReservationService _sut;

    public ReleaseReservationServiceTests()
    {
        _sut = new ReleaseReservationService(_store, _idempotency, NullLogger<ReleaseReservationService>.Instance);
    }

    private static ReleaseReservationRequest Request(decimal amount = 400m, string paymentId = "PAY-1") => new()
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
    public async Task Restores_budget_and_clears_reservation_when_reserve_was_active()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns(new ReserveBudgetResult { Reserved = true, RemainingBudget = 600m });
        _store.LoadAsync("marketing-2026Q2").Returns(Snapshot(600m));
        _store.TryWriteAsync(Arg.Any<DepartmentBudget>(), "etag-1").Returns(true);

        var result = await _sut.HandleAsync(Request(400m));

        Assert.True(result.Released);
        Assert.Equal(1000m, result.RemainingBudget);
        await _store.Received(1).TryWriteAsync(Arg.Is<DepartmentBudget>(b => b.RemainingUsd == 1000m), "etag-1");
        await _idempotency.Received(1).SaveReserveResultAsync(
            "PAY-1",
            Arg.Is<ReserveBudgetResult>(r => !r.Reserved && r.RemainingBudget == 1000m));
    }

    [Fact]
    public async Task Skips_when_no_active_reservation_exists()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns((ReserveBudgetResult?)null);

        var result = await _sut.HandleAsync(Request());

        Assert.False(result.Released);
        await _store.DidNotReceive().LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().TryWriteAsync(Arg.Any<DepartmentBudget>(), Arg.Any<string>());
        await _idempotency.DidNotReceive().SaveReserveResultAsync(Arg.Any<string>(), Arg.Any<ReserveBudgetResult>());
    }

    [Fact]
    public async Task Skips_when_reserve_was_refused()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns(new ReserveBudgetResult
        {
            Reserved = false,
            RemainingBudget = 100m,
            Outcome = ApprovalFlow.Contracts.Enums.PaymentOutcome.InsufficientBudget
        });

        var result = await _sut.HandleAsync(Request());

        Assert.False(result.Released);
        await _store.DidNotReceive().TryWriteAsync(Arg.Any<DepartmentBudget>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Replays_prior_release_without_touching_budget()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns(new ReserveBudgetResult
        {
            Reserved = false,
            RemainingBudget = 1000m,
            Reason = "release"
        });

        var result = await _sut.HandleAsync(Request());

        Assert.True(result.Released);
        Assert.Equal(1000m, result.RemainingBudget);
        await _store.DidNotReceive().LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().TryWriteAsync(Arg.Any<DepartmentBudget>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Retries_on_etag_conflict_then_succeeds()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns(new ReserveBudgetResult { Reserved = true, RemainingBudget = 600m });
        _store.LoadAsync("marketing-2026Q2").Returns(Snapshot(600m, "etag-a"), Snapshot(700m, "etag-b"));
        _store.TryWriteAsync(Arg.Any<DepartmentBudget>(), "etag-a").Returns(false);
        _store.TryWriteAsync(Arg.Any<DepartmentBudget>(), "etag-b").Returns(true);

        var result = await _sut.HandleAsync(Request(400m));

        Assert.True(result.Released);
        Assert.Equal(1100m, result.RemainingBudget);
        await _store.Received(2).LoadAsync("marketing-2026Q2", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_department_missing()
    {
        _idempotency.GetReserveResultAsync("PAY-1").Returns(new ReserveBudgetResult { Reserved = true, RemainingBudget = 100m });
        _store.LoadAsync("marketing-2026Q2").Returns((BudgetSnapshot?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandleAsync(Request()));
    }

    [Fact]
    public async Task Rejects_non_positive_amount()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.HandleAsync(Request(0m)));
    }
}
