using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Application.Services;
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
    public async Task Existing_trackingId_is_no_op()
    {
        _repo.ExistsByTrackingIdAsync("TRK-1", Arg.Any<CancellationToken>()).Returns(true);

        await _sut.HandleAsync(Event(Route.HumanReview), CancellationToken.None);

        await _repo.DidNotReceive().AddAsync(Arg.Any<ApprovalFlow.Approval.Domain.Entities.WorkflowInstance>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishReviewStatusAsync(Arg.Any<ReviewStatusV1>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishItemFinalizedAsync(Arg.Any<ItemFinalizedV1>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HumanReview_persists_and_publishes_review_status()
    {
        _repo.ExistsByTrackingIdAsync("TRK-1", Arg.Any<CancellationToken>()).Returns(false);

        await _sut.HandleAsync(Event(Route.HumanReview), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<ApprovalFlow.Approval.Domain.Entities.WorkflowInstance>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishReviewStatusAsync(
            Arg.Is<ReviewStatusV1>(e => e.TrackingId == "TRK-1" && e.SubState == ReviewSubState.AwaitingApproval),
            Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishItemFinalizedAsync(Arg.Any<ItemFinalizedV1>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoApprove_persists_and_publishes_item_finalized_paid()
    {
        _repo.ExistsByTrackingIdAsync("TRK-1", Arg.Any<CancellationToken>()).Returns(false);

        await _sut.HandleAsync(Event(Route.AutoApprove), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<ApprovalFlow.Approval.Domain.Entities.WorkflowInstance>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishItemFinalizedAsync(
            Arg.Is<ItemFinalizedV1>(e => e.TrackingId == "TRK-1" && e.FinalStatus == LifecycleStatus.Paid),
            Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishReviewStatusAsync(Arg.Any<ReviewStatusV1>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_persists_and_publishes_item_finalized_duplicate()
    {
        _repo.ExistsByTrackingIdAsync("TRK-1", Arg.Any<CancellationToken>()).Returns(false);

        await _sut.HandleAsync(Event(Route.Duplicate), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<ApprovalFlow.Approval.Domain.Entities.WorkflowInstance>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishItemFinalizedAsync(
            Arg.Is<ItemFinalizedV1>(e => e.TrackingId == "TRK-1" && e.FinalStatus == LifecycleStatus.Duplicate),
            Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishReviewStatusAsync(Arg.Any<ReviewStatusV1>(), Arg.Any<CancellationToken>());
    }
}
