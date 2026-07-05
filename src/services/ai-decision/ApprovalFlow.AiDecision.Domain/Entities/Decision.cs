namespace ApprovalFlow.AiDecision.Domain.Entities;

/// <summary>
/// Immutable audit record — one row per decided item (§4, §11). Owns the router's route plus the agent's
/// advisory recommendation, confidence, cited rules and fraud signal, keyed by <c>TrackingId</c> so the full
/// trail joins by <c>CorrelationId</c>. Category and route are stored as <c>int</c> so the enum can evolve
/// without a migration.
/// </summary>
public sealed class Decision
{
    public Guid Id { get; private set; }
    public string TrackingId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public int Route { get; private set; }
    public int Recommendation { get; private set; }
    public double Confidence { get; private set; }
    public decimal AmountUsd { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public string CitedRulesJson { get; private set; } = "[]";
    public bool FraudDetected { get; private set; }
    public string? FraudReason { get; private set; }
    public string PolicyVersion { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private Decision() { }

    public static Decision Create(
        string trackingId,
        string correlationId,
        int route,
        int recommendation,
        double confidence,
        decimal amountUsd,
        string department,
        string citedRulesJson,
        bool fraudDetected,
        string? fraudReason,
        string policyVersion)
    {
        return new Decision
        {
            Id = Guid.NewGuid(),
            TrackingId = trackingId,
            CorrelationId = correlationId,
            Route = route,
            Recommendation = recommendation,
            Confidence = confidence,
            AmountUsd = amountUsd,
            Department = department,
            CitedRulesJson = citedRulesJson,
            FraudDetected = fraudDetected,
            FraudReason = fraudReason,
            PolicyVersion = policyVersion,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
