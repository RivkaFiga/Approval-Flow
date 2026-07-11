using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Events.V1;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Approval.Application.Services;

/// <summary>
/// Workflow launcher for <c>decision.made</c> (§9): schedules a durable Dapr Workflow instance for fresh
/// events; skips redeliveries by checking for an existing <see cref="IWorkflowInstanceRepository"/> entry.
/// Persistence and publishing live inside the workflow itself (via activities).
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
            "Scheduled workflow for TrackingId {TrackingId} (route {Route}).",
            @event.TrackingId, @event.Route);
    }
}