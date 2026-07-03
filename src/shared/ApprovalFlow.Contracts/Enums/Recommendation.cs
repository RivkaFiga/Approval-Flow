namespace ApprovalFlow.Contracts.Enums;

/// <summary>
/// Advisory recommendation emitted by the AI agent (§7). Audit-only — it never authorizes a route.
/// The deterministic router alone decides the <see cref="Route"/>; a <c>Recommend.Approve</c> never
/// overrides a hard-stop, and a <c>Recommend.Reject</c> is distinct from <see cref="Route.Reject"/>
/// (which is only emitted for deterministically-detected high-severity violations such as MEAL-03).
/// </summary>
public enum Recommendation
{
    Approve,
    Reject
}
