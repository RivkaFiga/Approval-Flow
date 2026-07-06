using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Approval.Domain.Entities;

/// <summary>
/// One row of the queryable pending-approvals projection (§9.1): inserted when the workflow durably
/// pauses on <c>WaitForExternalEvent("ApprovalDecision")</c>, deleted when the approver's action
/// resumes the instance. Backs <c>GET /queue</c> (F4) because Dapr Workflow has no list-by-status query.
/// The durable source of truth is the Dapr Workflow instance itself; this is a read model.
/// </summary>
public sealed class PendingApproval
{
    public Guid Id { get; private set; }
    public string TrackingId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public int AgentRecommendation { get; private set; }
    public double Confidence { get; private set; }
    public string CitedRulesJson { get; private set; } = "[]";
    public decimal AmountUsd { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public DateTimeOffset EscalatedAt { get; private set; }

    private PendingApproval() { }

    public static PendingApproval Create(
        string trackingId,
        string correlationId,
        Recommendation agentRecommendation,
        double confidence,
        string citedRulesJson,
        decimal amountUsd,
        string department)
    {
        return new PendingApproval
        {
            Id = Guid.NewGuid(),
            TrackingId = trackingId,
            CorrelationId = correlationId,
            AgentRecommendation = (int)agentRecommendation,
            Confidence = confidence,
            CitedRulesJson = citedRulesJson,
            AmountUsd = amountUsd,
            Department = department,
            EscalatedAt = DateTimeOffset.UtcNow
        };
    }
}
