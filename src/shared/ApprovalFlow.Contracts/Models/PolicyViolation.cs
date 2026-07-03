namespace ApprovalFlow.Contracts.Models;

/// <summary>
/// A cited policy rule. From the agent these are advisory / audit-only and can never flip a route (§7.1);
/// the router also records the deterministically-detected rule ids behind a route for the audit trail.
/// </summary>
public sealed record PolicyViolation
{
    /// <summary>Stable policy rule id, e.g. <c>MEAL-02</c>, <c>SAAS-01</c>, <c>GLOBAL-DUP</c>.</summary>
    public string RuleId { get; init; } = string.Empty;

    public string? Detail { get; init; }
}
