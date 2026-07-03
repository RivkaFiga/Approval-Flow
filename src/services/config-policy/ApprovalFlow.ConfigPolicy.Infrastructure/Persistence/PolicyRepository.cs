using ApprovalFlow.ConfigPolicy.Application.Ports;
using ApprovalFlow.ConfigPolicy.Domain.Entities;
using ApprovalFlow.ConfigPolicy.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.ConfigPolicy.Infrastructure.Persistence;

public sealed class PolicyRepository : IPolicyRepository
{
    private readonly ConfigPolicyDbContext _db;

    public PolicyRepository(ConfigPolicyDbContext db) => _db = db;

    public async Task<PolicyDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.PolicyDocuments
            .Include(p => p.FxRates)
            .Include(p => p.KnownVendors)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<PolicyDocument?> GetActiveAsync(CancellationToken ct = default)
    {
        return await _db.PolicyDocuments
            .Include(p => p.FxRates)
            .Include(p => p.KnownVendors)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<PolicyDocument>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.PolicyDocuments
            .Include(p => p.FxRates)
            .Include(p => p.KnownVendors)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(PolicyDocument document, CancellationToken ct = default)
    {
        await _db.PolicyDocuments.AddAsync(document, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.FirstOrDefault();
            if (entry?.Entity is PolicyDocument doc)
                throw new ConcurrencyConflictException(doc.Id);
            throw;
        }
    }
}
