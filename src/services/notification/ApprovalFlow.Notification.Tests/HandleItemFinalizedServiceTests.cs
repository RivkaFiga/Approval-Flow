using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Application.Services;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Notification.Tests;

public class HandleItemFinalizedServiceTests
{
    private readonly ISubmissionStatusRepository _repo = Substitute.For<ISubmissionStatusRepository>();
    private readonly HandleItemFinalizedService _sut;

    public HandleItemFinalizedServiceTests()
    {
        _sut = new HandleItemFinalizedService(_repo, NullLogger<HandleItemFinalizedService>.Instance);
    }

    private static ItemFinalizedV1 Event(
        LifecycleStatus finalStatus,
        string reason,
        PaymentOutcome? outcome,
        DateTimeOffset occurredAt) => new()
    {
        TrackingId = "TRK-1",
        CorrelationId = "corr-1",
        OccurredAt = occurredAt,
        FinalStatus = finalStatus,
        Reason = reason,
        PaymentOutcome = outcome,
        ApprovalPath = ApprovalPath.Auto,
        AmountUsd = 100m
    };

    [Fact]
    public async Task Paid_finalization_records_terminal_status_and_payment_outcome()
    {
        var existing = SubmissionStatus.CreateReceived("TRK-1", "corr-1", DateTimeOffset.UtcNow);
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(LifecycleStatus.Paid, "Auto-approved.", PaymentOutcome.Paid, DateTimeOffset.UtcNow.AddSeconds(1)), CancellationToken.None);

        Assert.Equal(LifecycleStatus.Paid, existing.CurrentStatus);
        Assert.Equal("Auto-approved.", existing.Reason);
        Assert.Equal(PaymentOutcome.Paid, existing.CurrentPaymentOutcome);
    }

    [Fact]
    public async Task Rejected_finalization_leaves_payment_outcome_null()
    {
        var existing = SubmissionStatus.CreateReceived("TRK-1", "corr-1", DateTimeOffset.UtcNow);
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(LifecycleStatus.Rejected, "Alcohol only.", null, DateTimeOffset.UtcNow.AddSeconds(1)), CancellationToken.None);

        Assert.Equal(LifecycleStatus.Rejected, existing.CurrentStatus);
        Assert.Null(existing.CurrentPaymentOutcome);
    }

    [Fact]
    public async Task Creates_projection_when_finalized_arrives_first()
    {
        _repo.GetByTrackingIdAsync("TRK-1").Returns((SubmissionStatus?)null);
        SubmissionStatus? added = null;
        await _repo.AddAsync(Arg.Do<SubmissionStatus>(s => added = s), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Event(LifecycleStatus.Duplicate, "Duplicate submission.", null, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(LifecycleStatus.Duplicate, added!.CurrentStatus);
    }
}
