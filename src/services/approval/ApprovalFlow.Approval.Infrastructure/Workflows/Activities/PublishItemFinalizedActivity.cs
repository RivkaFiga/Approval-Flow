using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Events.V1;
using Dapr.Workflow;

namespace ApprovalFlow.Approval.Infrastructure.Workflows.Activities;

/// <summary>
/// Publishes <c>item.finalized</c> (§5.2) when the workflow reaches a terminal state. Stamps
/// <c>OccurredAt</c> at activity time so orchestrator replay stays deterministic.
/// </summary>
public sealed class PublishItemFinalizedActivity : WorkflowActivity<ItemFinalizedPublishRequest, object?>
{
    private readonly IWorkflowEventPublisher _publisher;

    public PublishItemFinalizedActivity(IWorkflowEventPublisher publisher) => _publisher = publisher;

    public override async Task<object?> RunAsync(WorkflowActivityContext context, ItemFinalizedPublishRequest input)
    {
        var @event = new ItemFinalizedV1
        {
            TrackingId = input.TrackingId,
            CorrelationId = input.CorrelationId,
            OccurredAt = DateTimeOffset.UtcNow,
            FinalStatus = input.FinalStatus,
            Reason = input.Reason,
            PaymentOutcome = input.PaymentOutcome,
            ApprovalPath = input.ApprovalPath,
            AmountUsd = input.AmountUsd
        };
        await _publisher.PublishItemFinalizedAsync(@event);
        return null;
    }
}
