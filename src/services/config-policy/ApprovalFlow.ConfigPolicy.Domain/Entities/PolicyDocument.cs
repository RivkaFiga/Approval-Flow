namespace ApprovalFlow.ConfigPolicy.Domain.Entities;

public sealed class PolicyDocument
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Markdown { get; private set; } = string.Empty;
    public decimal AutonomyCeilingUsd { get; private set; }
    public double AutonomyMinConfidence { get; private set; }
    public string BaseCurrency { get; private set; } = "USD";
    public int Version { get; private set; }
    public uint RowVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<FxRateEntry> _fxRates = new();
    public IReadOnlyList<FxRateEntry> FxRates => _fxRates.AsReadOnly();

    private readonly List<KnownVendorEntry> _knownVendors = new();
    public IReadOnlyList<KnownVendorEntry> KnownVendors => _knownVendors.AsReadOnly();

    private PolicyDocument() { }

    public static PolicyDocument Create(
        string name,
        string markdown,
        decimal autonomyCeilingUsd,
        double autonomyMinConfidence,
        string baseCurrency,
        IEnumerable<FxRateEntry> fxRates,
        IEnumerable<KnownVendorEntry> knownVendors)
    {
        Validate(name, markdown, autonomyCeilingUsd, autonomyMinConfidence);

        var now = DateTimeOffset.UtcNow;
        var doc = new PolicyDocument
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Markdown = markdown,
            AutonomyCeilingUsd = autonomyCeilingUsd,
            AutonomyMinConfidence = autonomyMinConfidence,
            BaseCurrency = baseCurrency.ToUpperInvariant(),
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };

        doc._fxRates.AddRange(fxRates);
        doc._knownVendors.AddRange(knownVendors);
        return doc;
    }

    public void Update(
        string name,
        string markdown,
        decimal autonomyCeilingUsd,
        double autonomyMinConfidence,
        string baseCurrency,
        IEnumerable<FxRateEntry> fxRates,
        IEnumerable<KnownVendorEntry> knownVendors)
    {
        Validate(name, markdown, autonomyCeilingUsd, autonomyMinConfidence);

        Name = name.Trim();
        Markdown = markdown;
        AutonomyCeilingUsd = autonomyCeilingUsd;
        AutonomyMinConfidence = autonomyMinConfidence;
        BaseCurrency = baseCurrency.ToUpperInvariant();
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;

        _fxRates.Clear();
        _fxRates.AddRange(fxRates);
        _knownVendors.Clear();
        _knownVendors.AddRange(knownVendors);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }

    private static void Validate(string name, string markdown, decimal ceiling, double confidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(markdown);
        if (ceiling < 0) throw new ArgumentOutOfRangeException(nameof(ceiling), "Autonomy ceiling must be non-negative.");
        if (confidence is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
    }
}
