using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.Contracts.Invocation.V1;

/// <summary>
/// Config/Policy read bundle for a decision — AI-Decision → Config/Policy (§5.1: fetch policy/thresholds/FX).
/// Hot-reloadable without a redeploy (F7, M13); <see cref="Version"/> changes when the policy is updated so a
/// cached snapshot can be invalidated (§5.3a).
/// </summary>
public sealed record PolicySnapshotResponse
{
    public string Version { get; init; } = string.Empty;
    public AutonomyThresholds Thresholds { get; init; } = new();

    public string BaseCurrency { get; init; } = "USD";

    /// <summary>FX rates to the base currency, keyed by ISO currency code (e.g. <c>EUR</c> → 1.08).</summary>
    public IReadOnlyDictionary<string, decimal> FxRates { get; init; } = new Dictionary<string, decimal>();

    /// <summary>Known/approved vendor names — the GLOBAL-VENDOR hard-stop input (§7).</summary>
    public IReadOnlyList<string> KnownVendors { get; init; } = Array.Empty<string>();

    /// <summary>Full policy markdown, served when RAG clause retrieval is not used (N5, §7.4).</summary>
    public string? PolicyMarkdown { get; init; }
}

/// <summary>Gateway → Config/Policy: tune the autonomy thresholds without a redeploy (F7, M13, §12.3).</summary>
public sealed record UpdateThresholdsRequest
{
    public AutonomyThresholds Thresholds { get; init; } = new();
}
