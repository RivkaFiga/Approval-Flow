using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Events.V1;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Approval.Application.Services;

/// <summary>
/// Handles one <c>decision.made</c> event (§9) by scheduling the durable <c>ApprovalWorkflow</c> keyed
/// by <c>trackingId</c>. The workflow owns persistence (<see cref="Ports.IWorkflowInstanceRepository"/>)
/// and event publishing (<c>review.status</c>, <c>item.finalized</c>) so the HITL resume path can raise
/// <c>ApprovalDecision</c> on the same durable instance keyed by <c>trackingId</c>.
///
/// Idempotent by <c>trackingId</c>: a redelivered <c>decision.made</c> for an already-tracked item is a
/// no-op (§10). Same-instance re-scheduling is also swallowed inside
/// <see cref="IApprovalWorkflowScheduler"/> so a race between the DB check and Dapr's own uniqueness
/// arbitration does not surface as a failure.
/// </summary>
public sealed class HandleDecisionMadeService
{
    private readonly IWorkflowInstanceRepository _repo;
    private readonly IApprovalWorkflowScheduler _scheduler;
    private readonly ILogger<HandleDecisionMadeService> _logger;

    public HandleDecisionMadeService(
        IWorkflowInstanceRepository repo,
        IApprovalWorkflowScheduler scheduler,
        ILogger<HandleDecisionMadeService> logger)
    {
        _repo = repo;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task HandleAsync(DecisionMadeV1 @event, CancellationToken ct = default)
    {
        if (await _repo.ExistsByTrackingIdAsync(@event.TrackingId, ct))
        {
            _logger.LogInformation(
                "Workflow already tracked for TrackingId {TrackingId}; skipping redelivery.",
                @event.TrackingId);
            return;
        }

        await _scheduler.ScheduleAsync(@event, ct);

        _logger.LogInformation(
            "Scheduled ApprovalWorkflow for TrackingId {TrackingId} (route {Route}).",
            @event.TrackingId, @event.Route);
    }
}
