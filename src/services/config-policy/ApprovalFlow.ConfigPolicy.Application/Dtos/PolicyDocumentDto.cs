namespace ApprovalFlow.ConfigPolicy.Application.Dtos;

public sealed record PolicyDocumentDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Markdown { get; init; } = string.Empty;
    public decimal AutonomyCeilingUsd { get; init; }
    public double AutonomyMinConfidence { get; init; }
    public string BaseCurrency { get; init; } = "USD";
    public int Version { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyDictionary<string, decimal> FxRates { get; init; } = new Dictionary<string, decimal>();
    public IReadOnlyList<string> KnownVendors { get; init; } = Array.Empty<string>();
}
