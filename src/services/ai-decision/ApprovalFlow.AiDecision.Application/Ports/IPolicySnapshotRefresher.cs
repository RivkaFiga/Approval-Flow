namespace ApprovalFlow.AiDecision.Application.Ports;

/// <summary>
/// Signals that the cached policy snapshot is stale and must be refetched from Config/Policy on the next
/// <see cref="IPolicySnapshotProvider.GetAsync"/> call (§5.3a). Keeps the pub/sub subscriber in Api decoupled
/// from the concrete Dapr-backed adapter in Infrastructure so a policy.changed event drives hot-reload
/// without leaking infrastructure types past this port.
/// </summary>
public interface IPolicySnapshotRefresher
{
    void Invalidate();
}
