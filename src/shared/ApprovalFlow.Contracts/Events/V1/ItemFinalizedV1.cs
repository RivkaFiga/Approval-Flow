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

    /// <summary>
    /// Owning department budget key (§8), e.g. <c>marketing-2026Q2</c>. Populated for items that ran through
    /// approval so the Payment saga can key its ETag CAS on the correct budget; empty for non-payment
    /// finalizations (rejected/duplicate).
    /// </summary>
    public string Department { get; init; } = string.Empty;
}
