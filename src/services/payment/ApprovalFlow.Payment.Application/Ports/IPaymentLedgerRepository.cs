using ApprovalFlow.Payment.Domain.Entities;

namespace ApprovalFlow.Payment.Application.Ports;

/// <summary>
/// Persistence port for the append-only payment ledger (§8, §11). Every committed charge writes exactly one
/// row; no updates, no deletes. The adapter enforces §10's exactly-once invariant with a UNIQUE constraint
/// on <see cref="PaymentLedgerEntry.PaymentId"/> — <see cref="TryAppendAsync"/> returns <c>false</c> when
/// the constraint fires, so the use case can replay the existing entry rather than double-charging.
/// </summary>
public interface IPaymentLedgerRepository
{
    /// <summary>
    /// Attempts to append <paramref name="entry"/>. Returns <c>true</c> when the row was persisted;
    /// <c>false</c> when the UNIQUE(<c>PaymentId</c>) constraint already had a row (concurrent Execute
    /// races through the application-level idempotency check — see <c>ExecutePaymentService</c>).
    /// </summary>
    Task<bool> TryAppendAsync(PaymentLedgerEntry entry, CancellationToken ct = default);

    Task<PaymentLedgerEntry?> GetByPaymentIdAsync(string paymentId, CancellationToken ct = default);
}
