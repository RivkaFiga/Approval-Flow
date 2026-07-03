using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Intake.Application.Ports;
using Dapr.Client;

namespace ApprovalFlow.Intake.Infrastructure.Events;

public sealed class DaprIntakeEventPublisher : IIntakeEventPublisher
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly DaprClient _dapr;

    public DaprIntakeEventPublisher(DaprClient dapr) => _dapr = dapr;

    public async Task PublishInvoiceSubmittedAsync(InvoiceSubmittedV1 @event, CancellationToken ct = default)
    {
        await _dapr.PublishEventAsync(PubSubName, EventTypes.InvoiceSubmitted, @event, ct);
    }
}
