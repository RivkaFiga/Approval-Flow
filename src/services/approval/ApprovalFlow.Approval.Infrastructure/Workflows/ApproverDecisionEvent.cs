using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Approval.Infrastructure.Workflows;

/// <summary>
/// Wire shape of the <c>ApprovalDecision</c> external event delivered to the durable workflow (§9).
/// The API layer never binds to this type — it goes through <c>IApprovalWorkflowEventRaiser</c>.
/// </summary>
public sealed record ApproverDecisionEvent
{
    public ApproverActionType Action { get; init; }
    public string ApproverId { get; init; } = string.Empty;
    public string? Comment { get; init; }
}
