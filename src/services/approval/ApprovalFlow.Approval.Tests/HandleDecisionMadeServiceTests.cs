using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Application.Services;
using ApprovalFlow.Approval.Domain.Entities;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Approval.Tests;

public class HandleDecisionMadeServiceTests
{
    private readonly IWorkflowInstanceRepository _repo = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IWorkflowEventPublisher _publisher = Substitute.For<IWorkflowEventPublisher>();
    private readonly HandleDecisionMadeService _sut;

    public HandleDecisionMadeServiceTests()
    {
        _sut = new HandleDecisionMadeService(_repo, _publisher, NullLogger<HandleDecisionMadeService>.Instance);
    }

    private static DecisionMadeV1 Event(Route route, string trackingId = "TRK-1") => new()
    {
        TrackingId = trackingId,
        CorrelationId = "corr-1",
        OccurredAt = DateTimeOffset.UtcNow,
        Route = route,
        Recommendation = Recommendation.Approve,
        Confidence = 0.9,
        AmountUsd = 199.99m,
        Department = "engineering-2026Q2",
        CitedRules = new[] { new PolicyViolation { RuleId = "SAAS-01", Detail = "Above monthly cap." } }
    };

    [Fact]
    public async Task HumanReview_publishes_ApprovalRequired_and_persists_instance()
    {
        _repo.ExistsByTrackingIdAsync("TRK-1").Returns(false);
        ReviewStatusV1? published = null;
        await _publisher.PublishReviewStatusAsync(Arg.Do<ReviewStatusV1>(e => published = e), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Event(Route.HumanReview), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishItemFinalizedAsync(Arg.Any<ItemFinalizedV1>(), Arg.Any<CancellationToken>());
        Assert.NotNull(published);
        Assert.Equal(ReviewSubState.AwaitingApproval, published!.SubState);
        Assert.Equal("TRK-1", published.TrackingId);
        Assert.Equal("corr-1", published.CorrelationId);
    }

    [Fact]
    public async Task AutoApprove_publishes_WorkflowCompleted_with_auto_path_and_paid_status()
    {
        _repo.ExistsByTrackingIdAsync(Arg.Any<string>()).Returns(false);
        ItemFinalizedV1? published = null;
        await _publisher.PublishItemFinalizedAsync(Arg.Do<ItemFinalizedV1>(e => published = e), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Event(Route.AutoApprove), CancellationToken.None);

        await _publisher.DidNotReceive().PublishReviewStatusAsync(Arg.Any<ReviewStatusV1>(), Arg.Any<CancellationToken>());
        Assert.NotNull(published);
        Assert.Equal(LifecycleStatus.Paid, published!.FinalStatus);
        Assert.Equal(PaymentOutcome.Paid, published.PaymentOutcome);
        Assert.Equal(ApprovalPath.Auto, published.ApprovalPath);
        Assert.Equal(199.99m, published.AmountUsd);
    }

    [Fact]
    public async Task Reject_publishes_WorkflowCompleted_with_rejected_status()
    {
        _repo.ExistsByTrackingIdAsync(Arg.Any<string>()).Returns(false);
        ItemFinalizedV1? published = null;
        await _publisher.PublishItemFinalizedAsync(Arg.Do<ItemFinalizedV1>(e => published = e), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Event(Route.Reject), CancellationToken.None);

        Assert.NotNull(published);
        Assert.Equal(LifecycleStatus.Rejected, published!.FinalStatus);
        Assert.Null(published.PaymentOutcome);
        Assert.Contains("SAAS-01", published.Reason);
    }

    [Fact]
    public async Task Duplicate_publishes_WorkflowCompleted_with_duplicate_status()
    {
        _repo.ExistsByTrackingIdAsync(Arg.Any<string>()).Returns(false);
        ItemFinalizedV1? published = null;
        await _publisher.PublishItemFinalizedAsync(Arg.Do<ItemFinalizedV1>(e => published = e), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Event(Route.Duplicate), CancellationToken.None);

        Assert.NotNull(published);
        Assert.Equal(LifecycleStatus.Duplicate, published!.FinalStatus);
        Assert.Null(published.PaymentOutcome);
    }

    [Fact]
    public async Task Redelivered_event_is_no_op()
    {
        _repo.ExistsByTrackingIdAsync("TRK-1").Returns(true);

        await _sut.HandleAsync(Event(Route.AutoApprove), CancellationToken.None);

        await _repo.DidNotReceive().AddAsync(Arg.Any<WorkflowInstance>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishReviewStatusAsync(Arg.Any<ReviewStatusV1>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishItemFinalizedAsync(Arg.Any<ItemFinalizedV1>(), Arg.Any<CancellationToken>());
    }
}
