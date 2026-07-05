using ApprovalFlow.AiDecision.Domain.Values;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Domain.Rules;

/// <summary>
/// The deterministic router (§7.1) — the single source of truth for the final <see cref="Route"/>. The
/// agent's <see cref="AgentRecommendation.Recommendation"/> and <see cref="AgentRecommendation.PolicyViolations"/>
/// can never flip a route; only <see cref="AgentRecommendation.Confidence"/> and
/// <see cref="AgentRecommendation.FraudSignal"/> feed the router — confidence strictly as an AND-gate floor,
/// fraud strictly as a hard stop. Provable ceiling: nothing can auto-approve above
/// <see cref="AutonomyThresholds.CeilingUsd"/> or under a hard stop, regardless of agent output (§7.2).
/// </summary>
public static class DecisionRouter
{
    public static RouteDecision Decide(
        PreCheckResult preChecks,
        CategoryRuleResult categoryRules,
        AgentRecommendation agent,
        AutonomyThresholds thresholds)
    {
        var cited = new List<PolicyViolation>();
        cited.AddRange(preChecks.HardStops);
        cited.AddRange(categoryRules.RejectViolations);
        cited.AddRange(categoryRules.EscalateViolations);

        FraudSignal? fraud = agent.FraudSignal is { Detected: true } ? agent.FraudSignal : null;

        if (fraud is not null)
        {
            cited.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.GlobalFraud,
                Detail = fraud.Reason ?? "Fraud signal detected."
            });
        }

        if (categoryRules.HasReject)
        {
            return Build(Route.Reject, agent, preChecks.AmountUsd, cited, fraud);
        }

        if (preChecks.HasHardStop || fraud is not null || categoryRules.EscalateViolations.Count > 0)
        {
            return Build(Route.HumanReview, agent, preChecks.AmountUsd, cited, fraud);
        }

        if (preChecks.AmountUsd > thresholds.CeilingUsd)
        {
            cited.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.AutonomyCeiling,
                Detail = $"Amount ${preChecks.AmountUsd:F2} exceeds ceiling ${thresholds.CeilingUsd:F2}."
            });
            return Build(Route.HumanReview, agent, preChecks.AmountUsd, cited, fraud);
        }

        if (agent.Confidence < thresholds.MinConfidence)
        {
            cited.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.AutonomyConfidence,
                Detail = $"Confidence {agent.Confidence:F2} below floor {thresholds.MinConfidence:F2}."
            });
            return Build(Route.HumanReview, agent, preChecks.AmountUsd, cited, fraud);
        }

        if (agent.Recommendation != Recommendation.Approve)
        {
            return Build(Route.HumanReview, agent, preChecks.AmountUsd, cited, fraud);
        }

        return Build(Route.AutoApprove, agent, preChecks.AmountUsd, cited, fraud);
    }

    private static RouteDecision Build(
        Route route,
        AgentRecommendation agent,
        decimal amountUsd,
        IReadOnlyList<PolicyViolation> cited,
        FraudSignal? fraud) => new()
    {
        Route = route,
        Recommendation = agent.Recommendation,
        Confidence = agent.Confidence,
        AmountUsd = amountUsd,
        CitedRules = cited,
        FraudSignal = fraud
    };
}
