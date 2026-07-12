using ApprovalFlow.AiDecision.Application.Ports;
using Dapr;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.AiDecision.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>policy.changed</c> (§5.3a). Busts the cached policy snapshot through the
/// <see cref="IPolicySnapshotRefresher"/> port so the next decision reads the new policy from Config/Policy
/// without a redeploy. The subscriber only knows about the port — the concrete Dapr-backed cache stays in
/// Infrastructure.
/// </summary>
[ApiController]
[Route("events")]
public sealed class PolicyChangedSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";
    private const string TopicName = "policy.changed";

    private readonly IPolicySnapshotRefresher _refresher;
    private readonly ILogger<PolicyChangedSubscriber> _logger;

    public PolicyChangedSubscriber(
        IPolicySnapshotRefresher refresher,
        ILogger<PolicyChangedSubscriber> logger)
    {
        _refresher = refresher;
        _logger = logger;
    }

    public sealed record PolicyChangedEvent(Guid PolicyId, int Version, DateTimeOffset OccurredAt);

    [Topic(PubSubName, TopicName)]
    [HttpPost("policy-changed")]
    public IActionResult Handle([FromBody] PolicyChangedEvent @event)
    {
        _logger.LogInformation(
            "Received policy.changed for PolicyId {PolicyId} version {Version}.",
            @event.PolicyId, @event.Version);

        _refresher.Invalidate();
        return Ok();
    }
}
