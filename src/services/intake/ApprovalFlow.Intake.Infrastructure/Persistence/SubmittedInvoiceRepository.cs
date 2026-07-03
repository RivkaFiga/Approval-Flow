using ApprovalFlow.Intake.Application.Ports;
using ApprovalFlow.Intake.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Intake.Infrastructure.Persistence;

public sealed class SubmittedInvoiceRepository : ISubmittedInvoiceRepository
{
    private readonly IntakeDbContext _db;

    public SubmittedInvoiceRepository(IntakeDbContext db) => _db = db;

    public Task<bool> ExistsByDedupKeyAsync(string dedupKey, CancellationToken ct = default)
        => _db.SubmittedInvoices.AnyAsync(x => x.DedupKey == dedupKey, ct);

    public async Task AddAsync(SubmittedInvoice invoice, CancellationToken ct = default)
        => await _db.SubmittedInvoices.AddAsync(invoice, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
