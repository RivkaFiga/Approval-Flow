using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Approval.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="IPendingApprovalRepository"/>. Backs the queryable F4 queue projection
/// (§9.1): insert on durable pause, delete on approver action.
/// </summary>
public sealed class PendingApprovalRepository : IPendingApprovalRepository
{
    private readonly ApprovalDbContext _db;

    public PendingApprovalRepository(ApprovalDbContext db) => _db = db;

    public Task<bool> ExistsByTrackingIdAsync(string trackingId, CancellationToken ct = default)
        => _db.PendingApprovals.AnyAsync(x => x.TrackingId == trackingId, ct);

    public async Task AddAsync(PendingApproval item, CancellationToken ct = default)
        => await _db.PendingApprovals.AddAsync(item, ct);

    public async Task RemoveByTrackingIdAsync(string trackingId, CancellationToken ct = default)
    {
        var existing = await _db.PendingApprovals
            .FirstOrDefaultAsync(x => x.TrackingId == trackingId, ct);
        if (existing is not null)
        {
            _db.PendingApprovals.Remove(existing);
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
