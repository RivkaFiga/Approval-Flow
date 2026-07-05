using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Application.Services;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Notification.Tests;

public class HandleDecisionMadeServiceTests
{
    private readonly ISubmissionStatusRepository _repo = Substitute.For<ISubmissionStatusRepository>();
    private readonly HandleDecisionMadeService _sut;

    public HandleDecisionMadeServiceTests()
    {
        _sut = new HandleDecisionMadeService(_repo, NullLogger<HandleDecisionMadeService>.Instance);
    }

    private static DecisionMadeV1 Event(Route route, DateTimeOffset occurredAt) => new()
    {
        TrackingId = "TRK-1",
        CorrelationId = "corr-1",
        OccurredAt = occurredAt,
        Route = route,
        Recommendation = Recommendation.Approve,
        Confidence = 0.9,
        AmountUsd = 199.99m,
        Department = "engineering-2026Q2"
    };

    [Fact]
    public async Task Advances_existing_projection_to_under_review()
    {
        var existing = SubmissionStatus.CreateReceived("TRK-1", "corr-1", new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero));
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(Route.HumanReview, new DateTimeOffset(2026, 7, 5, 12, 0, 1, TimeSpan.Zero)), CancellationToken.None);

        await _repo.DidNotReceive().AddAsync(Arg.Any<SubmissionStatus>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal(LifecycleStatus.UnderReview, existing.CurrentStatus);
        Assert.Equal(Route.HumanReview, existing.CurrentRoute);
        Assert.Equal(199.99m, existing.AmountUsd);
    }

    [Fact]
    public async Task Creates_projection_when_invoice_submitted_arrives_out_of_order()
    {
        _repo.GetByTrackingIdAsync("TRK-1").Returns((SubmissionStatus?)null);
        SubmissionStatus? added = null;
        await _repo.AddAsync(Arg.Do<SubmissionStatus>(s => added = s), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Event(Route.AutoApprove, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal("TRK-1", added!.TrackingId);
        Assert.Equal(LifecycleStatus.UnderReview, added.CurrentStatus);
        Assert.Equal(Route.AutoApprove, added.CurrentRoute);
    }

    [Fact]
    public async Task Older_OccurredAt_replay_does_not_regress_state()
    {
        var existing = SubmissionStatus.CreateReceived("TRK-1", "corr-1", new DateTimeOffset(2026, 7, 5, 12, 0, 5, TimeSpan.Zero));
        existing.ApplyFinalized(LifecycleStatus.Paid, "Paid.", PaymentOutcome.Paid, new DateTimeOffset(2026, 7, 5, 12, 0, 10, TimeSpan.Zero));
        _repo.GetByTrackingIdAsync("TRK-1").Returns(existing);

        await _sut.HandleAsync(Event(Route.HumanReview, new DateTimeOffset(2026, 7, 5, 12, 0, 1, TimeSpan.Zero)), CancellationToken.None);

        Assert.Equal(LifecycleStatus.Paid, existing.CurrentStatus);
    }
}
