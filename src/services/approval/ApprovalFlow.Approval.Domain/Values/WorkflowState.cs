namespace ApprovalFlow.Approval.Domain.Values;

/// <summary>
/// Lifecycle state of a single workflow instance owned by Approval/Workflow (§4, §9). Stored on the
/// instance so a restart can rehydrate the pause point; distinct from the submitter-facing
/// <see cref="ApprovalFlow.Contracts.Enums.LifecycleStatus"/> which is a Notification projection.
/// </summary>
public enum WorkflowState
{
    /// <summary>Router routed the item to <c>human_review</c>; workflow is paused waiting on the approver (§9).</summary>
    AwaitingApproval,

    /// <summary>Router routed the item to <c>auto_approve</c>; workflow completed the decision phase (§7.1).</summary>
    AutoApproved,

    /// <summary>Router deterministically rejected the item (§7.1, e.g. <c>MEAL-03</c>).</summary>
    Rejected,

    /// <summary>Router flagged the item as a duplicate re-submission (§7.1, <c>GLOBAL-DUP</c>).</summary>
    Duplicated
}
