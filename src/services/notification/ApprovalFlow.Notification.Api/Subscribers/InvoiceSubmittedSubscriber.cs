using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Services;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Notification.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>invoice.submitted</c> (§5.2). Records the initial <c>received</c>
/// projection row so F2 is live from the moment intake accepts. Idempotent by <c>trackingId</c> (§10).
/// </summary>
[ApiController]
[Route("events")]
public sealed class InvoiceSubmittedSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly HandleInvoiceSubmittedService _service;
    private readonly ILogger<InvoiceSubmittedSubscriber> _logger;

    public InvoiceSubmittedSubscriber(
        HandleInvoiceSubmittedService service,
        ILogger<InvoiceSubmittedSubscriber> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Topic(PubSubName, EventTypes.InvoiceSubmitted)]
    [HttpPost("invoice-submitted")]
    public async Task<IActionResult> Handle([FromBody] InvoiceSubmittedV1 @event, CancellationToken ct)
    {
        // Stitch the event's correlationId onto every downstream log line so the pipeline is joinable
        // end-to-end (F9/G6) — same pattern the Approval subscribers use.
        using (LogContext.PushProperty("CorrelationId", @event.CorrelationId))
        using (LogContext.PushProperty("TrackingId", @event.TrackingId))
        {
            _logger.LogInformation(
                "Received invoice.submitted for TrackingId {TrackingId}.", @event.TrackingId);
            await _service.HandleAsync(@event, ct);
        }
        return Ok();
    }
}
