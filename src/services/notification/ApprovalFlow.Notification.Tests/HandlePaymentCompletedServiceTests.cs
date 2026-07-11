using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Application.Services;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Notification.Tests;

/// <summary>
/// Verifies that <see cref="HandlePaymentCompletedService"/> advances the projection from
/// <see cref="LifecycleStatus.Paying"/> to the correct terminal status based on the saga outcome.
/// </summary>
public class HandlePaymentCompletedServiceTests
{
    private readonly ISubmissionStatusRepository _repo = Substitute.For<ISubmissionStatusRepository>();
    private readonly HandlePaymentCompletedService _sut;

    private static readonly DateTimeOffset T0 = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddSeconds(5);

    public HandlePaymentCompletedServiceTests()
    {
        _sut = new HandlePaymentCompletedService(_repo, NullLogger<HandlePaymentCompletedService>.Instance);
    }

    private static PaymentCompletedV1 Event(PaymentOutcome outcome, string? reason = null) => new()
    {
        TrackingId  = "TRK-1",
        CorrelationId = "corr-1",
        PaymentId   = "pay-TRK-1",
        OccurredAt  = T1,
        Department  = "engineering-2026Q2",
        AmountUsd   = 199.99m,
        Outcome     = outcome,
        Reason      = reason
    };

    private static SubmissionStatus ExistingPaying()
    {
        var s = SubmissionStatus.CreateReceived("TRK-1", "corr-1", T0);
        s.ApplyFinalized(LifecycleStatus.Paying, "Auto-approved.", PaymentOutcome.Paid, T0.AddSeconds(1));
        return s;
    }

    [Fact]
    public async Task Paid_outcome_projects_Paid_status()
    {
        var existing = ExistingPaying();
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(PaymentOutcome.Paid));

        Assert.Equal(LifecycleStatus.Paid, existing.CurrentStatus);
        Assert.Equal(PaymentOutcome.Paid, existing.CurrentPaymentOutcome);
    }

    [Fact]
    public async Task PaymentFailed_outcome_projects_PaymentFailed_status()
    {
        var existing = ExistingPaying();
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(PaymentOutcome.PaymentFailed, "Provider declined."));

        Assert.Equal(LifecycleStatus.PaymentFailed, existing.CurrentStatus);
        Assert.Equal("Provider declined.", existing.Reason);
    }

    [Fact]
    public async Task InsufficientBudget_outcome_projects_PaymentFailed_status()
    {
        var existing = ExistingPaying();
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(PaymentOutcome.InsufficientBudget, "Insufficient department budget."));

        Assert.Equal(LifecycleStatus.PaymentFailed, existing.CurrentStatus);
    }

    [Fact]
    public async Task Creates_projection_when_payment_completed_arrives_before_item_finalized()
    {
        _repo.GetByTrackingIdAsync("TRK-1").Returns((SubmissionStatus?)null);
        SubmissionStatus? added = null;
        await _repo.AddAsync(Arg.Do<SubmissionStatus>(s => added = s), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Event(PaymentOutcome.Paid));

        Assert.NotNull(added);
        Assert.Equal(LifecycleStatus.Paid, added!.CurrentStatus);
    }

    [Fact]
    public async Task Older_event_does_not_overwrite_newer_projection()
    {
        var existing = ExistingPaying();
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        // Stale event with OccurredAt before the current UpdatedAt
        var stale = Event(PaymentOutcome.PaymentFailed) with { OccurredAt = T0.AddMilliseconds(-1) };
        await _sut.HandleAsync(stale);

        // Status should still be Paying (the stale event was a no-op)
        Assert.Equal(LifecycleStatus.Paying, existing.CurrentStatus);
    }

    [Fact]
    public async Task Payment_failure_does_not_finalize_as_Paid()
    {
        var existing = ExistingPaying();
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(PaymentOutcome.PaymentFailed, "simulated failure"));

        Assert.NotEqual(LifecycleStatus.Paid, existing.CurrentStatus);
        Assert.Equal(LifecycleStatus.PaymentFailed, existing.CurrentStatus);
    }
}
