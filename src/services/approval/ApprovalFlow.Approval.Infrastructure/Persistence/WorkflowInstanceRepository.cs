using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Approval.Infrastructure.Persistence;

/// <summary>
/// EF Core adapter for <see cref="IWorkflowInstanceRepository"/>. First-writer-wins per <c>TrackingId</c>
/// is enforced by the UNIQUE index in <see cref="ApprovalDbContext"/>.
/// </summary>
public sealed class WorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly ApprovalDbContext _db;

    public WorkflowInstanceRepository(ApprovalDbContext db) => _db = db;

    public Task<bool> ExistsByTrackingIdAsync(string trackingId, CancellationToken ct = default)
        => _db.WorkflowInstances.AnyAsync(x => x.TrackingId == trackingId, ct);

    public async Task AddAsync(WorkflowInstance instance, CancellationToken ct = default)
        => await _db.WorkflowInstances.AddAsync(instance, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
