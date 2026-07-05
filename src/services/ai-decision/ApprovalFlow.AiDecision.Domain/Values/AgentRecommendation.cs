using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Domain.Values;

/// <summary>
/// Advisory output of the LLM agent (§7). The router consumes <see cref="Confidence"/> only as an AND-gate
/// floor and the <see cref="FraudSignal"/> as a hard-stop input; <see cref="PolicyViolations"/> is audit-only
/// and can never flip a route.
/// </summary>
public sealed record AgentRecommendation
{
    public Recommendation Recommendation { get; init; }

    /// <summary>0..1 self-reported confidence.</summary>
    public double Confidence { get; init; }

    public FraudSignal? FraudSignal { get; init; }

    public IReadOnlyList<PolicyViolation> PolicyViolations { get; init; } = Array.Empty<PolicyViolation>();
}
