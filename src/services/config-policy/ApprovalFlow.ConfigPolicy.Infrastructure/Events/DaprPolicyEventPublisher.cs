using ApprovalFlow.ConfigPolicy.Application.Ports;
using Dapr.Client;

namespace ApprovalFlow.ConfigPolicy.Infrastructure.Events;

public sealed class DaprPolicyEventPublisher : IPolicyEventPublisher
{
    private const string PubSubName = "approvalflow-pubsub";
    private const string TopicName = "policy.changed";

    private readonly DaprClient _dapr;

    public DaprPolicyEventPublisher(DaprClient dapr) => _dapr = dapr;

    public async Task PublishPolicyChangedAsync(Guid policyId, int version, CancellationToken ct = default)
    {
        var payload = new
        {
            Type = TopicName,
            SchemaVersion = 1,
            PolicyId = policyId,
            Version = version,
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _dapr.PublishEventAsync(PubSubName, TopicName, payload, ct);
    }
}
