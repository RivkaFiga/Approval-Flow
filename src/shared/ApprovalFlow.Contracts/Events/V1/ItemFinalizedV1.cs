using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Contracts.Events.V1;

/// <summary>
/// <c>item.finalized</c> — published by Approval/Workflow, consumed by Notification (§5.2). Terminal outcome
/// plus the <see cref="ApprovalPath"/> and <see cref="AmountUsd"/> that let F8 split money auto- vs.
/// human-approved (§12.2).
/// </summary>
public sealed record ItemFinalizedV1 : IntegrationEvent
{
    public override string Type => EventTypes.ItemFinalized;
    public override int SchemaVersion => 1;

    public string TrackingId { get; init; } = string.Empty;

    /// <summary>Terminal status (paid | rejected | payment-failed | duplicate).</summary>
    public LifecycleStatus FinalStatus { get; init; }

    /// <summary>Plain-language reason surfaced to the submitter (F2).</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>Payment result when the item ran the saga; null for non-payment finalizations.</summary>
    public PaymentOutcome? PaymentOutcome { get; init; }

    public ApprovalPath ApprovalPath { get; init; }

    public decimal AmountUsd { get; init; }
}
