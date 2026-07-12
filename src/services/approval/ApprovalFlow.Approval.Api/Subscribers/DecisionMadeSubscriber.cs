using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Approval.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>decision.made</c> (§5.2). Schedules a durable
/// <see cref="ApprovalFlow.Approval.Infrastructure.Workflows.ApprovalWorkflow"/> instance keyed by
/// <c>trackingId</c> via <see cref="IApprovalWorkflowScheduler"/>. Idempotent by <c>trackingId</c> (§10) —
/// the scheduler swallows "already exists" so a redelivery is a no-op.
/// </summary>
[ApiController]
[Route("events")]
public sealed class DecisionMadeSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly IApprovalWorkflowScheduler _scheduler;
    private readonly ILogger<DecisionMadeSubscriber> _logger;

    public DecisionMadeSubscriber(
        IApprovalWorkflowScheduler scheduler,
        ILogger<DecisionMadeSubscriber> logger)
    {
        _scheduler = scheduler;
        _logger = logger;
    }

    [Topic(PubSubName, EventTypes.DecisionMade)]
    [HttpPost("decision-made")]
    public async Task<IActionResult> Handle([FromBody] DecisionMadeV1 @event, CancellationToken ct)
    {
        // Stitch the event's correlationId (not the fresh one CorrelationIdMiddleware generates for
        // this Dapr POST) onto every downstream log line so the pipeline is joinable end-to-end (F9/G6).
        using (LogContext.PushProperty("CorrelationId", @event.CorrelationId))
        using (LogContext.PushProperty("TrackingId", @event.TrackingId))
        {
            _logger.LogInformation(
                "Scheduling ApprovalWorkflow for TrackingId {TrackingId} (route {Route}).",
                @event.TrackingId, @event.Route);
            await _scheduler.ScheduleAsync(@event, ct);
        }
        return Ok();
    }
}
