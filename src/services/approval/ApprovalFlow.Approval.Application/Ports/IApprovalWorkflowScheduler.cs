using ApprovalFlow.Contracts.Events.V1;

namespace ApprovalFlow.Approval.Application.Ports;

/// <summary>
/// Starts a durable workflow instance for a <c>decision.made</c> event (§9). The instance id equals the
/// item's <c>trackingId</c> so HITL endpoints can raise events on the same instance without an extra lookup.
/// </summary>
public interface IApprovalWorkflowScheduler
{
    Task ScheduleAsync(DecisionMadeV1 input, CancellationToken ct = default);
}
