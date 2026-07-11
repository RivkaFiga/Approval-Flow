using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Services;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Notification.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>payment.completed</c> (§8). Advances the status projection from
/// <c>Paying</c> to the actual terminal outcome (<c>Paid</c> / <c>PaymentFailed</c>). Idempotent by
/// monotonic <c>OccurredAt</c> (§10).
/// </summary>
[ApiController]
[Route("events")]
public sealed class PaymentCompletedSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly HandlePaymentCompletedService _service;
    private readonly ILogger<PaymentCompletedSubscriber> _logger;

    public PaymentCompletedSubscriber(
        HandlePaymentCompletedService service,
        ILogger<PaymentCompletedSubscriber> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Topic(PubSubName, EventTypes.PaymentCompleted)]
    [HttpPost("payment-completed")]
    public async Task<IActionResult> Handle([FromBody] PaymentCompletedV1 @event, CancellationToken ct)
    {
        using (LogContext.PushProperty("CorrelationId", @event.CorrelationId))
        using (LogContext.PushProperty("TrackingId", @event.TrackingId))
        {
            _logger.LogInformation(
                "Received payment.completed for TrackingId {TrackingId} (outcome {Outcome}).",
                @event.TrackingId, @event.Outcome);
            await _service.HandleAsync(@event, ct);
        }
        return Ok();
    }
}
