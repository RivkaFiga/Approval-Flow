using ApprovalFlow.AiDecision.Application.Ports;
using ApprovalFlow.AiDecision.Domain.Values;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Infrastructure.Agents;

/// <summary>
/// Deterministic stub agent behind the LLM anti-corruption layer (§12.6, ADR-006). Always returns
/// <c>Approve @ 0.9</c> with no fraud signal, so router behaviour depends entirely on deterministic checks
/// — the provable-ceiling posture (§7.2). A real Gemini adapter will replace this by config (M15) without
/// changing Application/Domain code.
/// </summary>
public sealed class StubPolicyAgent : IPolicyAgent
{
    public Task<AgentRecommendation> RecommendAsync(
        Invoice invoice,
        PolicySnapshotResponse policy,
        CancellationToken ct = default)
    {
        var recommendation = new AgentRecommendation
        {
            Recommendation = Recommendation.Approve,
            Confidence = 0.9,
            FraudSignal = null,
            PolicyViolations = Array.Empty<PolicyViolation>()
        };

        return Task.FromResult(recommendation);
    }
}
