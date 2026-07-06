using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Payment.Application.Services;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Payment.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>item.finalized</c> — the end-to-end trigger for the payment leg
/// (<c>ItemFinalized → Payment → PaymentCompleted</c>, §5.2 / §8). Delegates the reserve/pay/compensate
/// saga to <see cref="HandleItemFinalizedService"/>; publishing of <c>payment.completed</c> is that
/// service's responsibility so this controller stays a thin adapter. At-least-once delivery is absorbed by
/// the saga's <c>trackingId</c> idempotency (§10).
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
                "Received item.finalized for TrackingId {TrackingId} (paymentOutcome {PaymentOutcome}).",
                @event.TrackingId, @event.PaymentOutcome);
            await _service.HandleAsync(@event, ct);
        }
        return Ok();
    }
}
