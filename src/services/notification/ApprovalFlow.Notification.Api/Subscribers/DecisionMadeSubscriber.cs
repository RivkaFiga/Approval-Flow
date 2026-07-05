using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Services;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Notification.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>decision.made</c> (§5.2). Advances the projection to <c>under-review</c>
/// and records the router's route + USD amount. Idempotent by monotonic <c>OccurredAt</c> (§10).
/// </summary>
[ApiController]
[Route("events")]
public sealed class DecisionMadeSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly HandleDecisionMadeService _service;
    private readonly ILogger<DecisionMadeSubscriber> _logger;

    public DecisionMadeSubscriber(
        HandleDecisionMadeService service,
        ILogger<DecisionMadeSubscriber> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Topic(PubSubName, EventTypes.DecisionMade)]
    [HttpPost("decision-made")]
    public async Task<IActionResult> Handle([FromBody] DecisionMadeV1 @event, CancellationToken ct)
    {
        using (LogContext.PushProperty("CorrelationId", @event.CorrelationId))
        using (LogContext.PushProperty("TrackingId", @event.TrackingId))
        {
            _logger.LogInformation(
                "Received decision.made for TrackingId {TrackingId} (route {Route}).",
                @event.TrackingId, @event.Route);
            await _service.HandleAsync(@event, ct);
        }
        return Ok();
    }
}
