using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Application.Services;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Notification.Tests;

public class HandleReviewStatusServiceTests
{
    private readonly ISubmissionStatusRepository _repo = Substitute.For<ISubmissionStatusRepository>();
    private readonly HandleReviewStatusService _sut;

    public HandleReviewStatusServiceTests()
    {
        _sut = new HandleReviewStatusService(_repo, NullLogger<HandleReviewStatusService>.Instance);
    }

    private static ReviewStatusV1 Event(ReviewSubState subState, DateTimeOffset occurredAt, string? whatWeStillNeed = null) => new()
    {
        TrackingId = "TRK-1",
        CorrelationId = "corr-1",
        OccurredAt = occurredAt,
        SubState = subState,
        WhatWeStillNeed = whatWeStillNeed
    };

    [Fact]
    public async Task AwaitingApproval_advances_state_without_reason()
    {
        var existing = SubmissionStatus.CreateReceived("TRK-1", "corr-1", DateTimeOffset.UtcNow);
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(ReviewSubState.AwaitingApproval, DateTimeOffset.UtcNow.AddSeconds(1)), CancellationToken.None);

        Assert.Equal(LifecycleStatus.AwaitingApproval, existing.CurrentStatus);
        Assert.Null(existing.Reason);
    }

    [Fact]
    public async Task AwaitingInfo_records_what_we_still_need()
    {
        var existing = SubmissionStatus.CreateReceived("TRK-1", "corr-1", DateTimeOffset.UtcNow);
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(ReviewSubState.AwaitingInfo, DateTimeOffset.UtcNow.AddSeconds(1), "Need attendee count."), CancellationToken.None);

        Assert.Equal(LifecycleStatus.AwaitingInfo, existing.CurrentStatus);
        Assert.Equal("Need attendee count.", existing.Reason);
    }

    [Fact]
    public async Task Creates_projection_when_missing()
    {
        _repo.GetByTrackingIdAsync("TRK-1").Returns((SubmissionStatus?)null);
        SubmissionStatus? added = null;
        await _repo.AddAsync(Arg.Do<SubmissionStatus>(s => added = s), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Event(ReviewSubState.Paying, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(LifecycleStatus.Paying, added!.CurrentStatus);
    }
}
