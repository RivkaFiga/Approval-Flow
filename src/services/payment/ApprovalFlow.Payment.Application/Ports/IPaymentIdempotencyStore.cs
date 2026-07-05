using ApprovalFlow.Contracts.Invocation.V1;

namespace ApprovalFlow.Payment.Application.Ports;

/// <summary>
/// Persistence port for the payment-idempotency records used by §10: a retried <c>reserve</c> against the
/// same <c>paymentId</c> returns the previous <see cref="ReserveBudgetResult"/> instead of drawing the
/// budget twice. Keyed by <c>paymentId</c> per <c>PaymentContracts</c>. Adapters implement this against the
/// Dapr state store.
/// </summary>
public interface IPaymentIdempotencyStore
{
    Task<ReserveBudgetResult?> GetReserveResultAsync(string paymentId, CancellationToken ct = default);

    Task SaveReserveResultAsync(string paymentId, ReserveBudgetResult result, CancellationToken ct = default);
}
