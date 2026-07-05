using ApprovalFlow.AiDecision.Application.Services;
using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using Dapr;
using Microsoft.AspNetCore.Mvc;

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

    public InvoiceSubmittedSubscriber(DecideInvoiceService service) => _service = service;

    [Topic(PubSubName, EventTypes.InvoiceSubmitted)]
    [HttpPost("invoice-submitted")]
    public async Task<IActionResult> Handle([FromBody] InvoiceSubmittedV1 @event, CancellationToken ct)
    {
        await _service.DecideAsync(@event, ct);
        return Ok();
    }
}
