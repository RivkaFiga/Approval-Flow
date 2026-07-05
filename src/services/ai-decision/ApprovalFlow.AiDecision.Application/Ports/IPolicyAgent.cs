using ApprovalFlow.AiDecision.Domain.Values;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Application.Ports;

/// <summary>
/// LLM agent anti-corruption layer (§12.6, ADR-006). Returns only advisory data
/// (<see cref="AgentRecommendation"/>) that the router consumes; provider swap = config change (M15).
/// Adapters must translate transient provider errors into a "no confident recommendation" outcome so the
/// router escalates rather than auto-approving on error (§7.2 fail-fast).
/// </summary>
public interface IPolicyAgent
{
    Task<AgentRecommendation> RecommendAsync(
        Invoice invoice,
        PolicySnapshotResponse policy,
        CancellationToken ct = default);
}
