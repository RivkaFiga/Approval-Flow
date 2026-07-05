using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using Dapr.Client;

namespace ApprovalFlow.Approval.Infrastructure.Events;

/// <summary>
/// Dapr pub/sub adapter for the workflow's outbound events (§5.2). The topic name matches the CloudEvent
/// <c>type</c> declared on the payload record, so consumers subscribe by the canonical id in
/// <see cref="EventTypes"/>.
/// </summary>
public sealed class DaprWorkflowEventPublisher : IWorkflowEventPublisher
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly DaprClient _dapr;

    public DaprWorkflowEventPublisher(DaprClient dapr) => _dapr = dapr;

    public Task PublishReviewStatusAsync(ReviewStatusV1 @event, CancellationToken ct = default)
        => _dapr.PublishEventAsync(PubSubName, EventTypes.ReviewStatus, @event, ct);

    public Task PublishItemFinalizedAsync(ItemFinalizedV1 @event, CancellationToken ct = default)
        => _dapr.PublishEventAsync(PubSubName, EventTypes.ItemFinalized, @event, ct);
}
