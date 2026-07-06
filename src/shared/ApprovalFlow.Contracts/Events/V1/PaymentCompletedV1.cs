using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Contracts.Events.V1;

/// <summary>
/// <c>payment.completed</c> — published by the Payment service after the reserve/pay/compensate saga (§8)
/// terminates. Terminal for the payment leg of an approved item: <see cref="PaymentOutcome.Paid"/> means the
/// ledger row is committed; the other outcomes carry the reason the saga rolled back.
/// </summary>
public sealed record PaymentCompletedV1 : IntegrationEvent
{
    public override string Type => EventTypes.PaymentCompleted;
    public override int SchemaVersion => 1;

    public string TrackingId { get; init; } = string.Empty;

    /// <summary>Idempotency key used across reserve + execute (§10).</summary>
    public string PaymentId { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public decimal AmountUsd { get; init; }

    public PaymentOutcome Outcome { get; init; }

    /// <summary>Immutable ledger entry id on success; null on failure (audit, §8).</summary>
    public string? LedgerEntryId { get; init; }

    /// <summary>Plain-language reason surfaced when the saga did not pay (compensated / insufficient).</summary>
    public string? Reason { get; init; }
}
