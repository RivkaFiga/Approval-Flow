namespace ApprovalFlow.Approval.Domain.Values;

/// <summary>
/// Deterministic outcome of the workflow decider (§9): what the Approval/Workflow service must do next
/// after ingesting a <c>decision.made</c> event. Mirrors the two publisher branches — <c>review.status</c>
/// on human review, <c>item.finalized</c> on any terminal route.
/// </summary>
public enum WorkflowNextStep
{
    /// <summary>Route is <c>human_review</c>: publish <c>review.status(awaiting-approval)</c> and durably pause (§9).</summary>
    RequireHumanApproval,

    /// <summary>Route is terminal (<c>auto_approve</c> | <c>reject</c> | <c>duplicate</c>): publish <c>item.finalized</c>.</summary>
    Complete
}
