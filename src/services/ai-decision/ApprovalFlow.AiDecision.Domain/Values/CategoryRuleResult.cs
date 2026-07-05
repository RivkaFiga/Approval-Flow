using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Domain.Values;

/// <summary>
/// Output of the deterministic CategoryRules evaluator (§7 C6/C7). <see cref="RejectViolations"/> collects
/// deterministically-detected high-severity rejects (e.g. MEAL-03 alcohol-only). <see cref="EscalateViolations"/>
/// collects rules that force <c>human_review</c> (over-cap, missing info). Compliance is <c>true</c> only when
/// both are empty.
/// </summary>
public sealed record CategoryRuleResult
{
    public IReadOnlyList<PolicyViolation> RejectViolations { get; init; } = Array.Empty<PolicyViolation>();
    public IReadOnlyList<PolicyViolation> EscalateViolations { get; init; } = Array.Empty<PolicyViolation>();

    public bool IsCompliant => RejectViolations.Count == 0 && EscalateViolations.Count == 0;
    public bool HasReject => RejectViolations.Count > 0;
}
