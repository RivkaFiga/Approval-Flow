using ApprovalFlow.AiDecision.Infrastructure.Policy;
using Dapr;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.AiDecision.Api.Subscribers;

/// <summary>
/// Dapr pub/sub subscription for <c>policy.changed</c> (§5.3a). Busts the cached
/// <see cref="DaprConfigPolicySnapshotProvider"/> snapshot so subsequent decisions read the new policy from
/// Config/Policy without a redeploy.
/// </summary>
[ApiController]
[Route("events")]
public sealed class PolicyChangedSubscriber : ControllerBase
{
    private const string PubSubName = "approvalflow-pubsub";
    private const string TopicName = "policy.changed";

    private readonly DaprConfigPolicySnapshotProvider _snapshotProvider;
    private readonly ILogger<PolicyChangedSubscriber> _logger;

    public PolicyChangedSubscriber(
        DaprConfigPolicySnapshotProvider snapshotProvider,
        ILogger<PolicyChangedSubscriber> logger)
    {
        _snapshotProvider = snapshotProvider;
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

        _snapshotProvider.Invalidate();
        return Ok();
    }
}
