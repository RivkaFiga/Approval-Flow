using ApprovalFlow.Payment.Domain.Values;

namespace ApprovalFlow.Payment.Domain.Entities;

/// <summary>
/// Saga aggregate for one approved item's payment leg (§8). Tracks the lifecycle
/// (<c>Reserved</c> → <c>Paid</c> | <c>Compensated</c> | <c>InsufficientBudget</c>) so a redelivered
/// <c>item.finalized</c> for the same <c>trackingId</c> is a no-op (§10, redelivery de-dup). The append-only
/// ledger row is still the authoritative audit record for a successful charge — this record is the saga's
/// own state and is what the subscriber reads to decide whether to run or replay.
///
/// Pure aggregate — no I/O and no framework types; EF Core reflects into the private setters through the
/// infrastructure mapping.
/// </summary>
public sealed class PaymentRecord
{
    public Guid Id { get; private set; }
    public string PaymentId { get; private set; } = string.Empty;
    public string TrackingId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public decimal AmountUsd { get; private set; }
    public PaymentRecordStatus Status { get; private set; }
    public string? LedgerEntryId { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PaymentRecord() { }

    public static PaymentRecord StartReserved(
        string paymentId,
        string trackingId,
        string correlationId,
        string department,
        decimal amountUsd,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            throw new ArgumentException("PaymentId is required.", nameof(paymentId));
        if (string.IsNullOrWhiteSpace(trackingId))
            throw new ArgumentException("TrackingId is required.", nameof(trackingId));
        if (string.IsNullOrWhiteSpace(department))
            throw new ArgumentException("Department is required.", nameof(department));
        if (amountUsd <= 0m)
            throw new ArgumentOutOfRangeException(nameof(amountUsd), amountUsd, "AmountUsd must be positive.");

        return new PaymentRecord
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            TrackingId = trackingId,
            CorrelationId = correlationId ?? string.Empty,
            Department = department,
            AmountUsd = amountUsd,
            Status = PaymentRecordStatus.Reserved,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkPaid(string ledgerEntryId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(ledgerEntryId))
            throw new ArgumentException("LedgerEntryId is required.", nameof(ledgerEntryId));

        Status = PaymentRecordStatus.Paid;
        LedgerEntryId = ledgerEntryId;
        Reason = null;
        UpdatedAt = now;
    }

    public void MarkCompensated(string reason, DateTimeOffset now)
    {
        Status = PaymentRecordStatus.Compensated;
        Reason = reason;
        LedgerEntryId = null;
        UpdatedAt = now;
    }

    public void MarkInsufficientBudget(string reason, DateTimeOffset now)
    {
        Status = PaymentRecordStatus.InsufficientBudget;
        Reason = reason;
        LedgerEntryId = null;
        UpdatedAt = now;
    }
}
