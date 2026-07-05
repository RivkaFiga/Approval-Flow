using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Infrastructure.Configuration;
using Dapr.Client;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Payment.Infrastructure.Persistence;

/// <summary>
/// Dapr-state adapter for <see cref="IPaymentIdempotencyStore"/>. Records reserve results keyed by
/// <c>paymentId</c> so a retried request returns the original outcome (§10). The record itself is the
/// <see cref="ReserveBudgetResult"/> contract type — no adapter DTO needed.
/// </summary>
public sealed class DaprPaymentIdempotencyStore : IPaymentIdempotencyStore
{
    private const string KeyPrefix = "payments|";

    private readonly DaprClient _dapr;
    private readonly PaymentInfrastructureOptions _options;

    public DaprPaymentIdempotencyStore(DaprClient dapr, IOptions<PaymentInfrastructureOptions> options)
    {
        _dapr = dapr;
        _options = options.Value;
    }

    public Task<ReserveBudgetResult?> GetReserveResultAsync(string paymentId, CancellationToken ct = default)
        => _dapr.GetStateAsync<ReserveBudgetResult?>(_options.StateStoreName, Key(paymentId), cancellationToken: ct);

    public Task SaveReserveResultAsync(string paymentId, ReserveBudgetResult result, CancellationToken ct = default)
        => _dapr.SaveStateAsync(_options.StateStoreName, Key(paymentId), result, cancellationToken: ct);

    private static string Key(string paymentId) => KeyPrefix + paymentId;
}
