using ApprovalFlow.AiDecision.Application.Services;
using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.AiDecision.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>invoice.submitted</c> (§5.2). Delegates the full decision pipeline
/// to <see cref="DecideInvoiceService"/>. Idempotent by <c>trackingId</c> (§10).
/// </summary>
[ApiController]
[Route("events")]
public sealed class InvoiceSubmittedSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly DecideInvoiceService _service;
    private readonly ILogger<InvoiceSubmittedSubscriber> _logger;

    public InvoiceSubmittedSubscriber(
        DecideInvoiceService service,
        ILogger<InvoiceSubmittedSubscriber> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Topic(PubSubName, EventTypes.InvoiceSubmitted)]
    [HttpPost("invoice-submitted")]
    public async Task<IActionResult> Handle([FromBody] InvoiceSubmittedV1 @event, CancellationToken ct)
    {
        // Stitch the event's correlationId (not the fresh one CorrelationIdMiddleware generates for
        // this Dapr POST) onto every downstream log line so the pipeline is joinable end-to-end (F9/G6).
        using (LogContext.PushProperty("CorrelationId", @event.CorrelationId))
        using (LogContext.PushProperty("TrackingId", @event.TrackingId))
        {
            _logger.LogInformation(
                "Received invoice.submitted for TrackingId {TrackingId}.",
                @event.TrackingId);
            await _service.DecideAsync(@event, ct);
        }
        return Ok();
    }
}
