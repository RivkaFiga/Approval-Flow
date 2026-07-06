using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Payment.Application.Ports;
using Dapr.Client;

namespace ApprovalFlow.Payment.Infrastructure.Events;

/// <summary>
/// Dapr pub/sub adapter for <see cref="IPaymentEventPublisher"/>. Mirrors the same
/// <c>approvalflow-pubsub</c> component + CloudEvent-<c>type</c> convention as the other services'
/// publishers (§5.2), so consumers subscribe by <see cref="EventTypes.PaymentCompleted"/>.
/// </summary>
public sealed class DaprPaymentEventPublisher : IPaymentEventPublisher
{
    private const string PubSubName = "approvalflow-pubsub";

    private readonly DaprClient _dapr;

    public DaprPaymentEventPublisher(DaprClient dapr) => _dapr = dapr;

    public Task PublishPaymentCompletedAsync(PaymentCompletedV1 @event, CancellationToken ct = default)
        => _dapr.PublishEventAsync(PubSubName, EventTypes.PaymentCompleted, @event, ct);
}
