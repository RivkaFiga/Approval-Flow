using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Payment.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="IPaymentRecordRepository"/>. Backs the redelivery-idempotent
/// <c>item.finalized</c> handler (§10) by keying on <c>trackingId</c>. Lives in the same Postgres schema as
/// the append-only ledger so a saga run and its ledger row can be joined in the audit trail (§11).
/// </summary>
public sealed class PaymentRecordRepository : IPaymentRecordRepository
{
    private readonly PaymentDbContext _db;

    public PaymentRecordRepository(PaymentDbContext db) => _db = db;

    public Task<PaymentRecord?> GetByTrackingIdAsync(string trackingId, CancellationToken ct = default)
        => _db.PaymentRecords.FirstOrDefaultAsync(x => x.TrackingId == trackingId, ct);

    public async Task AddAsync(PaymentRecord record, CancellationToken ct = default)
        => await _db.PaymentRecords.AddAsync(record, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
