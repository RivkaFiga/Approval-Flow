using ApprovalFlow.Approval.Application.Ports;
using Dapr.Workflow;

namespace ApprovalFlow.Approval.Infrastructure.Workflows;

/// <summary>
/// Dapr Workflow adapter for <see cref="IApprovalWorkflowEventRaiser"/>. Translates the framework-free
/// <see cref="ApproverDecisionPayload"/> into the Dapr-facing <see cref="ApproverDecisionEvent"/> and
/// raises it on the durable instance keyed by <c>trackingId</c> (§9).
/// </summary>
public sealed class DaprApprovalWorkflowEventRaiser : IApprovalWorkflowEventRaiser
{
    private readonly DaprWorkflowClient _client;

    public DaprApprovalWorkflowEventRaiser(DaprWorkflowClient client) => _client = client;

    public Task RaiseApprovalDecisionAsync(
        string trackingId,
        ApproverDecisionPayload payload,
        CancellationToken ct = default)
    {
        var wire = new ApproverDecisionEvent
        {
            Action = payload.Action,
            ApproverId = payload.ApproverId,
            Comment = payload.Comment
        };
        return _client.RaiseEventAsync(
            trackingId,
            ApprovalWorkflow.ApprovalDecisionEventName,
            wire,
            ct);
    }
}
