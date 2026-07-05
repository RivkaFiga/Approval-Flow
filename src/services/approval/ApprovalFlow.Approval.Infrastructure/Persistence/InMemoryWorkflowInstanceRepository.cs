using System.Collections.Concurrent;
using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Domain.Entities;

namespace ApprovalFlow.Approval.Infrastructure.Persistence;

/// <summary>
/// Minimal in-memory store for <see cref="WorkflowInstance"/>. Registered as a singleton so the map
/// survives request scopes but not process restarts — durable persistence (Dapr Workflow instance +
/// Postgres pending-approvals projection, §9.1, §11) is a later slice. Concurrent-safe by
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>; the pending queue in <see cref="_pending"/> flushes on
/// <see cref="SaveChangesAsync"/> so it mirrors the DbContext unit-of-work semantics the other services use.
/// </summary>
public sealed class InMemoryWorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly ConcurrentDictionary<string, WorkflowInstance> _byTrackingId = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<WorkflowInstance> _pending = new();

    public Task<bool> ExistsByTrackingIdAsync(string trackingId, CancellationToken ct = default)
        => Task.FromResult(_byTrackingId.ContainsKey(trackingId));

    public Task AddAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        _pending.Enqueue(instance);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        while (_pending.TryDequeue(out var instance))
        {
            // First writer wins per TrackingId (mirrors the UNIQUE constraint the durable adapter will have).
            _byTrackingId.TryAdd(instance.TrackingId, instance);
        }
        return Task.CompletedTask;
    }
}
