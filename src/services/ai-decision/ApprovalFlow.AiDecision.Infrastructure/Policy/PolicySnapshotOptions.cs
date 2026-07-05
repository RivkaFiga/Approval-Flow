namespace ApprovalFlow.AiDecision.Infrastructure.Policy;

/// <summary>
/// Bound from the <c>PolicySnapshot</c> configuration section. Placeholder used until the Config/Policy
/// service adapter is wired — the same <see cref="ApprovalFlow.AiDecision.Application.Ports.IPolicySnapshotProvider"/>
/// port will be re-bound to a Dapr-invocation adapter without touching Application/Domain code.
/// </summary>
public sealed class PolicySnapshotOptions
{
    public const string SectionName = "PolicySnapshot";

    public string Version { get; set; } = "policy-v1";
    public string BaseCurrency { get; set; } = "USD";
    public decimal CeilingUsd { get; set; } = 250m;
    public double MinConfidence { get; set; } = 0.80;
    public Dictionary<string, decimal> FxRates { get; set; } = new();
    public List<string> KnownVendors { get; set; } = new();
}
