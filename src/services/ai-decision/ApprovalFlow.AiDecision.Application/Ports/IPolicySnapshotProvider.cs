using ApprovalFlow.Contracts.Invocation.V1;

namespace ApprovalFlow.AiDecision.Application.Ports;

/// <summary>
/// Reads the current policy bundle — thresholds, FX rates, known vendors — from Config/Policy (§5.1). Cached
/// with a short TTL and invalidated by a <c>policy.updated</c> signal (§5.3a). The adapter belongs in
/// Infrastructure; a Config/Policy outage must not stall every decision.
/// </summary>
public interface IPolicySnapshotProvider
{
    Task<PolicySnapshotResponse> GetAsync(CancellationToken ct = default);
}
