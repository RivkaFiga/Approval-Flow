using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Services;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Notification.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>item.finalized</c> (§5.2). Records the terminal status +
/// plain-language reason so <c>GET /status</c> returns the final outcome F2 needs. Idempotent by monotonic
/// <c>OccurredAt</c> (§10).
/// </summary>
[ApiController]
[Route("events")]
public sealed class ItemFinalizedSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly HandleItemFinalizedService _service;
    private readonly ILogger<ItemFinalizedSubscriber> _logger;

    public ItemFinalizedSubscriber(
        HandleItemFinalizedService service,
        ILogger<ItemFinalizedSubscriber> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Topic(PubSubName, EventTypes.ItemFinalized)]
    [HttpPost("item-finalized")]
    public async Task<IActionResult> Handle([FromBody] ItemFinalizedV1 @event, CancellationToken ct)
    {
        using (LogContext.PushProperty("CorrelationId", @event.CorrelationId))
        using (LogContext.PushProperty("TrackingId", @event.TrackingId))
        {
            _logger.LogInformation(
                "Received item.finalized for TrackingId {TrackingId} (finalStatus {FinalStatus}).",
                @event.TrackingId, @event.FinalStatus);
            await _service.HandleAsync(@event, ct);
        }
        return Ok();
    }
}
