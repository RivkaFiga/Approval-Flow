using ApprovalFlow.Payment.Domain.Entities;

namespace ApprovalFlow.Payment.Application.Ports;

/// <summary>
/// Persistence port for the payment saga aggregate <see cref="PaymentRecord"/>. The record's lifecycle is what
/// makes the <c>item.finalized</c> subscriber idempotent (§10): if a record already exists for a
/// <c>trackingId</c> the saga is skipped and the prior outcome is replayed. Adapters live in infrastructure
/// (EF Core against Postgres — same store as the append-only ledger, §11).
/// </summary>
public interface IPaymentRecordRepository
{
    Task<PaymentRecord?> GetByTrackingIdAsync(string trackingId, CancellationToken ct = default);

    Task AddAsync(PaymentRecord record, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
