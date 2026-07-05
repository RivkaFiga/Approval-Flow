using ApprovalFlow.AiDecision.Domain.Entities;

namespace ApprovalFlow.AiDecision.Application.Ports;

public interface IDecisionRepository
{
    Task<bool> ExistsByTrackingIdAsync(string trackingId, CancellationToken ct = default);
    Task AddAsync(Decision decision, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
