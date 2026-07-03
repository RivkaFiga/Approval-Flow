using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.Contracts.Invocation.V1;

/// <summary>Gateway → Workflow: the approver's single action on a pending item (F5, §9).</summary>
public sealed record ApproverActionRequest
{
    public string TrackingId { get; init; } = string.Empty;
    public ApproverActionType Action { get; init; }

    /// <summary>Identity of the acting approver, recorded with the raised workflow event (§9).</summary>
    public string ApproverId { get; init; } = string.Empty;

    /// <summary>Reason (for reject) or the "what we still need" note (for send-back).</summary>
    public string? Comment { get; init; }
}

/// <summary>Workflow → Gateway: acknowledgement of an approver action and the resulting status.</summary>
public sealed record ApproverActionResponse
{
    public string TrackingId { get; init; } = string.Empty;
    public LifecycleStatus Status { get; init; }
}

/// <summary>
/// A row in the approver queue (F4), served from the pending-approvals projection owned by Workflow (§9.1).
/// </summary>
public sealed record PendingApprovalDto
{
    public string TrackingId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public Recommendation AgentRecommendation { get; init; }
    public double Confidence { get; init; }
    public IReadOnlyList<string> CitedRuleIds { get; init; } = Array.Empty<string>();
    public decimal AmountUsd { get; init; }
    public string Department { get; init; } = string.Empty;
    public string Submitter { get; init; } = string.Empty;
    public DateTimeOffset EscalatedAt { get; init; }
}

/// <summary>Gateway → Workflow: the approver queue (F4), sortable by amount or age.</summary>
public sealed record ApproverQueueResponse
{
    public IReadOnlyList<PendingApprovalDto> Items { get; init; } = Array.Empty<PendingApprovalDto>();
}

/// <summary>
/// Gateway → Workflow: the submitter supplies missing info for an <c>awaiting-info</c> item, keyed by the
/// immutable <see cref="TrackingId"/> (raises <c>InfoProvided</c> on the same instance, §9). This is not a
/// new <c>POST /invoices</c>, so Intake's business-key dedup is never consulted.
/// </summary>
public sealed record ProvideInfoRequest
{
    public string TrackingId { get; init; } = string.Empty;
    public string? Notes { get; init; }

    /// <summary>Corrected invoice, if the correction changes amounts/details (a correction may change total, §9).</summary>
    public Invoice? CorrectedInvoice { get; init; }
}
