using ApprovalFlow.AiDecision.Application.Ports;
using ApprovalFlow.AiDecision.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.AiDecision.Infrastructure.Persistence;

public sealed class DecisionRepository : IDecisionRepository
{
    private readonly AiDecisionDbContext _db;

    public DecisionRepository(AiDecisionDbContext db) => _db = db;

    public Task<bool> ExistsByTrackingIdAsync(string trackingId, CancellationToken ct = default)
        => _db.Decisions.AnyAsync(x => x.TrackingId == trackingId, ct);

    public async Task AddAsync(Decision decision, CancellationToken ct = default)
        => await _db.Decisions.AddAsync(decision, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
