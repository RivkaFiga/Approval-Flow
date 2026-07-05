using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApprovalFlow.Payment.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="IPaymentLedgerRepository"/>. Append-only: only <c>AddAsync</c> is used —
/// no updates, no deletes (§8, §11). Translates a Postgres UNIQUE-violation (SQLSTATE 23505) on
/// <c>PaymentId</c> into <c>TryAppendAsync = false</c> so the caller can replay the winning row.
/// </summary>
public sealed class PaymentLedgerRepository : IPaymentLedgerRepository
{
    private const string PostgresUniqueViolationSqlState = "23505";

    private readonly PaymentDbContext _db;

    public PaymentLedgerRepository(PaymentDbContext db) => _db = db;

    public async Task<bool> TryAppendAsync(PaymentLedgerEntry entry, CancellationToken ct = default)
    {
        await _db.PaymentLedgerEntries.AddAsync(entry, ct);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Detach the failed row so a subsequent read via the same context isn't polluted.
            _db.Entry(entry).State = EntityState.Detached;
            return false;
        }
    }

    public Task<PaymentLedgerEntry?> GetByPaymentIdAsync(string paymentId, CancellationToken ct = default)
        => _db.PaymentLedgerEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct);

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == PostgresUniqueViolationSqlState;
}
