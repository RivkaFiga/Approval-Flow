using ApprovalFlow.Approval.Application.Services;
using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using Dapr;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.Approval.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>decision.made</c> (§5.2). Delegates the full ingest/decide/publish
/// pipeline to <see cref="HandleDecisionMadeService"/>. Idempotent by <c>trackingId</c> (§10).
/// </summary>
[ApiController]
[Route("events")]
public sealed class DecisionMadeSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly HandleDecisionMadeService _service;

    public DecisionMadeSubscriber(HandleDecisionMadeService service) => _service = service;

    [Topic(PubSubName, EventTypes.DecisionMade)]
    [HttpPost("decision-made")]
    public async Task<IActionResult> Handle([FromBody] DecisionMadeV1 @event, CancellationToken ct)
    {
        await _service.HandleAsync(@event, ct);
        return Ok();
    }
}
