namespace ApprovalFlow.Contracts.Models;

/// <summary>
/// Autonomy thresholds (policy.md §6). Read by the deterministic router at decision time so they are
/// tunable without a redeploy (F7, M13, §12.3).
/// </summary>
public sealed record AutonomyThresholds
{
    /// <summary><c>AUTONOMY-CEILING</c> — the max USD amount eligible for auto-approve (default $250).</summary>
    public decimal CeilingUsd { get; init; }

    /// <summary><c>AUTONOMY-CONFIDENCE</c> — the min agent confidence for auto-approve (default 0.80).</summary>
    public double MinConfidence { get; init; }
}
