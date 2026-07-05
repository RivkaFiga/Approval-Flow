using ApprovalFlow.Payment.Domain.Values;

namespace ApprovalFlow.Payment.Application.Ports;

/// <summary>
/// Anti-corruption layer for the payment provider (§1.4, §12.6). The application depends only on this
/// interface; concrete implementations (the simulated provider today, a real rail later) live in
/// infrastructure and can be swapped by DI without touching the use case. Implementations MUST honour the
/// idempotency key on <see cref="ChargeCommand.PaymentId"/> — the simulated provider does this trivially
/// because it has no side effects, but a real provider adapter is expected to pass the key to the rail.
/// </summary>
public interface IPaymentProvider
{
    Task<ChargeResult> ChargeAsync(ChargeCommand command, CancellationToken ct = default);
}
