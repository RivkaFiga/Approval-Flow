namespace ApprovalFlow.Payment.Domain.Entities;

/// <summary>
/// One immutable row in the append-only payment ledger (§8, §11). The ledger is Payment's audit record —
/// each entry represents exactly one committed charge. <see cref="PaymentId"/> carries a UNIQUE database
/// constraint enforced by infrastructure, so §10's "exactly one payment per idempotency key" invariant
/// holds even if two Execute calls race past the application-level check.
/// </summary>
public sealed class PaymentLedgerEntry
{
    public Guid Id { get; private set; }
    public string PaymentId { get; private set; } = string.Empty;
    public string TrackingId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public decimal AmountUsd { get; private set; }

    /// <summary>Opaque reference returned by the payment provider (audit trail — §12.1/F9).</summary>
    public string ProviderReference { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    private PaymentLedgerEntry() { }

    public static PaymentLedgerEntry Create(
        string paymentId,
        string trackingId,
        string correlationId,
        string department,
        decimal amountUsd,
        string providerReference,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            throw new ArgumentException("PaymentId is required.", nameof(paymentId));
        if (string.IsNullOrWhiteSpace(trackingId))
            throw new ArgumentException("TrackingId is required.", nameof(trackingId));
        if (string.IsNullOrWhiteSpace(department))
            throw new ArgumentException("Department is required.", nameof(department));
        if (string.IsNullOrWhiteSpace(providerReference))
            throw new ArgumentException("ProviderReference is required.", nameof(providerReference));
        if (amountUsd <= 0m)
            throw new ArgumentOutOfRangeException(nameof(amountUsd), amountUsd, "AmountUsd must be positive.");

        return new PaymentLedgerEntry
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            TrackingId = trackingId,
            CorrelationId = correlationId ?? string.Empty,
            Department = department,
            AmountUsd = amountUsd,
            ProviderReference = providerReference,
            CreatedAt = createdAt
        };
    }
}
