using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Events.V1;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Approval.Application.Services;

/// <summary>
/// Launches the durable Dapr Workflow for one <c>decision.made</c> pass (§9). Idempotent by
/// <c>trackingId</c> — a redelivered event whose workflow has already been scheduled is a no-op (§10). All
/// persistence and outbound event publishing (<c>review.status</c>, <c>item.finalized</c>) happen inside
/// the workflow itself so pause/resume state survives a restart (M11).
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
            "Workflow scheduled for TrackingId {TrackingId} (route {Route}).",
            @event.TrackingId, @event.Route);
    }
}
