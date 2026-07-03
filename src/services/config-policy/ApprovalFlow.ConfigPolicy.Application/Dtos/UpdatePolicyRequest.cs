namespace ApprovalFlow.ConfigPolicy.Application.Dtos;

public sealed record UpdatePolicyRequest
{
    public string Name { get; init; } = string.Empty;
    public string Markdown { get; init; } = string.Empty;
    public decimal AutonomyCeilingUsd { get; init; } = 250m;
    public double AutonomyMinConfidence { get; init; } = 0.80;
    public string BaseCurrency { get; init; } = "USD";
    public Dictionary<string, decimal> FxRates { get; init; } = new();
    public List<string> KnownVendors { get; init; } = new();
    public int ExpectedVersion { get; init; }
}
