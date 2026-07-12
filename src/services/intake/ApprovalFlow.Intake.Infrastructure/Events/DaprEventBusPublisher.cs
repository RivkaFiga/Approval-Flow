using System.Text.Json;
using ApprovalFlow.Intake.Application.Ports;
using Dapr.Client;

namespace ApprovalFlow.Intake.Infrastructure.Events;

/// <summary>
/// Publishes staged outbox messages through the Dapr pubsub component. The Dapr event contract
/// (topic name + JSON payload) is unchanged from the previous direct-publish implementation.
/// </summary>
public sealed class DaprEventBusPublisher : IEventBusPublisher
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly DaprClient _dapr;

    public DaprEventBusPublisher(DaprClient dapr) => _dapr = dapr;

    public async Task PublishAsync(string eventType, string payloadJson, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        await _dapr.PublishEventAsync(PubSubName, eventType, doc.RootElement.Clone(), ct);
    }
}
