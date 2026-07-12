using ApprovalFlow.Intake.Application.Ports;
using ApprovalFlow.Intake.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Intake.Infrastructure.Persistence;

public sealed class OutboxDispatchStore : IOutboxDispatchStore
{
    private readonly IntakeDbContext _db;

    public OutboxDispatchStore(IntakeDbContext db) => _db = db;

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken ct = default)
    {
        return await _db.OutboxMessages
            .Where(m => m.DispatchedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(maxCount)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
