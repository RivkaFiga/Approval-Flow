using ApprovalFlow.Approval.Domain.Values;
using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Approval.Domain.Entities;

/// <summary>
/// One workflow instance per submitted item (§4, §9). Persists the router's decision and the current
/// <see cref="Values.WorkflowState"/> so a restart could rehydrate the pause point. In this slice the store
/// is minimal/in-memory; the durable substrate (Dapr Workflow instance + history, §11) lands in a later
/// slice. <see cref="Route"/> and <see cref="State"/> are stored as <c>int</c> so the enums can evolve
/// without a migration.
/// </summary>
public sealed class WorkflowInstance
{
    public Guid Id { get; private set; }
    public string TrackingId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public int Route { get; private set; }
    public int State { get; private set; }
    public int Recommendation { get; private set; }
    public double Confidence { get; private set; }
    public decimal AmountUsd { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public string CitedRulesJson { get; private set; } = "[]";
    public bool FraudDetected { get; private set; }
    public string? FraudReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private WorkflowInstance() { }

    public static WorkflowInstance Create(
        string trackingId,
        string correlationId,
        Route route,
        WorkflowState state,
        Recommendation recommendation,
        double confidence,
        decimal amountUsd,
        string department,
        string citedRulesJson,
        bool fraudDetected,
        string? fraudReason)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            TrackingId = trackingId,
            CorrelationId = correlationId,
            Route = (int)route,
            State = (int)state,
            Recommendation = (int)recommendation,
            Confidence = confidence,
            AmountUsd = amountUsd,
            Department = department,
            CitedRulesJson = citedRulesJson,
            FraudDetected = fraudDetected,
            FraudReason = fraudReason,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
