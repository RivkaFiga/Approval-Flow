using ApprovalFlow.Approval.Domain.Entities;

namespace ApprovalFlow.Approval.Application.Ports;

/// <summary>
/// Persistence port for the queryable pending-approvals projection owned by Workflow (§9.1). Inserted on
/// durable pause, removed on approver action — first-writer-wins by <c>trackingId</c>.
/// </summary>
public interface IPendingApprovalRepository
{
    Task<IReadOnlyList<PendingApproval>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsByTrackingIdAsync(string trackingId, CancellationToken ct = default);
    Task AddAsync(PendingApproval item, CancellationToken ct = default);
    Task RemoveByTrackingIdAsync(string trackingId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
