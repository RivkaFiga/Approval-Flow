using ApprovalFlow.Approval.Domain.Entities;

namespace ApprovalFlow.Approval.Application.Ports;

/// <summary>
/// Persistence port for <see cref="WorkflowInstance"/>. In this slice it is backed by an in-memory store;
/// a durable adapter (Dapr Workflow instance + a queryable Postgres projection, §9.1) can replace it
/// without touching the application layer.
/// </summary>
public interface IWorkflowInstanceRepository
{
    Task<bool> ExistsByTrackingIdAsync(string trackingId, CancellationToken ct = default);
    Task AddAsync(WorkflowInstance instance, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
