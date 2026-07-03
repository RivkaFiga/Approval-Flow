using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.Contracts.Events.V1;

/// <summary>
/// <c>decision.made</c> — published by AI-Decision, consumed by Approval/Workflow and Notification (§5.2).
/// The route is the deterministic router's output; the recommendation, confidence and cited rules are audit
/// data (the agent only recommends, §7). Carries the USD amount and department the payment saga needs (§8).
/// </summary>
public sealed record DecisionMadeV1 : IntegrationEvent
{
    public override string Type => EventTypes.DecisionMade;
    public override int SchemaVersion => 1;

    public string TrackingId { get; init; } = string.Empty;

    /// <summary>Final deterministic route (§7.1).</summary>
    public Route Route { get; init; }

    /// <summary>Agent's advisory recommendation (audit only).</summary>
    public Recommendation Recommendation { get; init; }

    /// <summary>Agent self-reported confidence 0..1 — an AND-gate floor, never an authorization (§7.2).</summary>
    public double Confidence { get; init; }

    /// <summary>Cited policy rule ids behind the route (audit / explanation).</summary>
    public IReadOnlyList<PolicyViolation> CitedRules { get; init; } = Array.Empty<PolicyViolation>();

    public FraudSignal? FraudSignal { get; init; }

    /// <summary>USD amount computed by the router from converted line items (§7.2).</summary>
    public decimal AmountUsd { get; init; }

    /// <summary>Owning department budget key for the saga (§8), e.g. <c>marketing-2026Q2</c>.</summary>
    public string Department { get; init; } = string.Empty;
}
