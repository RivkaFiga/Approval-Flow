using ApprovalFlow.AiDecision.Application.Ports;
using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using Dapr.Client;

namespace ApprovalFlow.AiDecision.Infrastructure.Events;

public sealed class DaprDecisionEventPublisher : IDecisionEventPublisher
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly DaprClient _dapr;

    public DaprDecisionEventPublisher(DaprClient dapr) => _dapr = dapr;

    public async Task PublishDecisionMadeAsync(DecisionMadeV1 @event, CancellationToken ct = default)
    {
        await _dapr.PublishEventAsync(PubSubName, EventTypes.DecisionMade, @event, ct);
    }
}
