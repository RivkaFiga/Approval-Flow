using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Domain.Values;

/// <summary>
/// Final output of the deterministic router (§7.1). The <see cref="Route"/> is authoritative; the agent's
/// advisory <see cref="Recommendation"/>/<see cref="Confidence"/> and <see cref="CitedRules"/> are audit data.
/// </summary>
public sealed record RouteDecision
{
    public Route Route { get; init; }
    public Recommendation Recommendation { get; init; }
    public double Confidence { get; init; }
    public decimal AmountUsd { get; init; }
    public IReadOnlyList<PolicyViolation> CitedRules { get; init; } = Array.Empty<PolicyViolation>();
    public FraudSignal? FraudSignal { get; init; }
}
