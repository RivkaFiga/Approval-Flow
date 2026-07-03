namespace ApprovalFlow.Contracts.Models;

/// <summary>
/// Fraud-pattern signal emitted by the agent (§7). A raised signal is a deterministic router hard stop
/// (GLOBAL-FRAUD) — the signal is recorded, never used as an authorization.
/// </summary>
public sealed record FraudSignal
{
    public bool Detected { get; init; }
    public string? Reason { get; init; }
}
