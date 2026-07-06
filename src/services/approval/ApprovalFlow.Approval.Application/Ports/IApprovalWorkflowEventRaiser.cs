namespace ApprovalFlow.Approval.Application.Ports;

/// <summary>
/// Raises an <c>ApprovalDecision</c> external event on the durable workflow instance identified by
/// <paramref name="trackingId"/> (§9). This is the HITL resume mechanism — approve / reject / send-back all
/// go through this single port, so the API layer never binds to a Dapr SDK type.
/// </summary>
public interface IApprovalWorkflowEventRaiser
{
    Task RaiseApprovalDecisionAsync(
        string trackingId,
        ApproverDecisionPayload payload,
        CancellationToken ct = default);
}
