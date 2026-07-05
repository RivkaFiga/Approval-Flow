using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Services;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Notification.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>review.status</c> (§5.2). Reflects HITL sub-state transitions
/// (awaiting-approval / awaiting-info / paying) on the projection so F2 stays live during the slow 20%
/// (INV-1003). Idempotent by monotonic <c>OccurredAt</c> (§10).
/// </summary>
[ApiController]
[Route("events")]
public sealed class ReviewStatusSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly HandleReviewStatusService _service;
    private readonly ILogger<ReviewStatusSubscriber> _logger;

    public ReviewStatusSubscriber(
        HandleReviewStatusService service,
        ILogger<ReviewStatusSubscriber> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Topic(PubSubName, EventTypes.ReviewStatus)]
    [HttpPost("review-status")]
    public async Task<IActionResult> Handle([FromBody] ReviewStatusV1 @event, CancellationToken ct)
    {
        using (LogContext.PushProperty("CorrelationId", @event.CorrelationId))
        using (LogContext.PushProperty("TrackingId", @event.TrackingId))
        {
            _logger.LogInformation(
                "Received review.status for TrackingId {TrackingId} (subState {SubState}).",
                @event.TrackingId, @event.SubState);
            await _service.HandleAsync(@event, ct);
        }
        return Ok();
    }
}
