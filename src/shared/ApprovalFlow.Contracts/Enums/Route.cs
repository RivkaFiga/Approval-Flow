namespace ApprovalFlow.Contracts.Enums;

/// <summary>
/// Final decision produced by the deterministic router (§7). It is never produced by the LLM.
/// Canonical wire values: <c>auto_approve</c>, <c>human_review</c>, <c>reject</c>, <c>duplicate</c>.
/// </summary>
public enum Route
{
    AutoApprove,
    HumanReview,
    Reject,
    Duplicate
}
