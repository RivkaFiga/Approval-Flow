using ApprovalFlow.Contracts.Events.V1;

namespace ApprovalFlow.Payment.Application.Ports;

/// <summary>
/// Publishing port for the single event the payment leg emits (§5.2): <c>payment.completed</c> — the
/// terminal result of the reserve/pay/compensate saga (§8). Kept as its own port so the application layer
/// stays free of Dapr types; the adapter lives in infrastructure.
/// </summary>
public interface IPaymentEventPublisher
{
    Task PublishPaymentCompletedAsync(PaymentCompletedV1 @event, CancellationToken ct = default);
}
