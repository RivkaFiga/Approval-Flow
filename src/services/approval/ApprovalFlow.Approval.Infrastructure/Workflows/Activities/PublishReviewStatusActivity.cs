using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Events.V1;
using Dapr.Workflow;

namespace ApprovalFlow.Approval.Infrastructure.Workflows.Activities;

/// <summary>
/// Publishes <c>review.status</c> (§5.2) for HITL sub-state transitions. Stamps <c>OccurredAt</c> at
/// activity time so orchestrator replay stays deterministic.
/// </summary>
public sealed class PublishReviewStatusActivity : WorkflowActivity<ReviewStatusPublishRequest, object?>
{
    private readonly IWorkflowEventPublisher _publisher;

    public PublishReviewStatusActivity(IWorkflowEventPublisher publisher) => _publisher = publisher;

    public override async Task<object?> RunAsync(WorkflowActivityContext context, ReviewStatusPublishRequest input)
    {
        var @event = new ReviewStatusV1
        {
            TrackingId = input.TrackingId,
            CorrelationId = input.CorrelationId,
            OccurredAt = DateTimeOffset.UtcNow,
            SubState = input.SubState,
            WhatWeStillNeed = input.WhatWeStillNeed
        };
        await _publisher.PublishReviewStatusAsync(@event);
        return null;
    }
}
