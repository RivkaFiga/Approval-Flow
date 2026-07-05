using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Domain.Values;

/// <summary>
/// Output of the deterministic pre-checks (§7 C1–C5): USD amount, whether the item math reconciles, and any
/// cited hard-stop rule ids (missing receipt, unknown vendor, FX hard stop, math mismatch).
/// </summary>
public sealed record PreCheckResult
{
    public decimal AmountUsd { get; init; }
    public bool MathReconciles { get; init; }
    public IReadOnlyList<PolicyViolation> HardStops { get; init; } = Array.Empty<PolicyViolation>();

    public bool HasHardStop => HardStops.Count > 0;
}
