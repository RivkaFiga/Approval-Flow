using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Infrastructure.Configuration;
using Dapr.Client;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Payment.Infrastructure.Persistence;

/// <summary>
/// Dapr-state adapter for <see cref="IPaymentIdempotencyStore"/>. Records reserve and execute results keyed
/// by <c>paymentId</c> so a retried request returns the original outcome (§10). The stored records are the
/// contract types themselves — no adapter DTO needed. Reserve and execute records live under distinct key
/// prefixes so they cannot collide.
/// </summary>
public sealed class DaprPaymentIdempotencyStore : IPaymentIdempotencyStore
{
    private const string ReserveKeyPrefix = "payments|reserve|";
    private const string ExecuteKeyPrefix = "payments|execute|";

    private readonly DaprClient _dapr;
    private readonly PaymentInfrastructureOptions _options;

    public DaprPaymentIdempotencyStore(DaprClient dapr, IOptions<PaymentInfrastructureOptions> options)
    {
        _dapr = dapr;
        _options = options.Value;
    }

    public Task<ReserveBudgetResult?> GetReserveResultAsync(string paymentId, CancellationToken ct = default)
        => _dapr.GetStateAsync<ReserveBudgetResult?>(_options.StateStoreName, ReserveKey(paymentId), cancellationToken: ct);

    public Task SaveReserveResultAsync(string paymentId, ReserveBudgetResult result, CancellationToken ct = default)
        => _dapr.SaveStateAsync(_options.StateStoreName, ReserveKey(paymentId), result, cancellationToken: ct);

    public Task<ExecutePaymentResult?> GetExecuteResultAsync(string paymentId, CancellationToken ct = default)
        => _dapr.GetStateAsync<ExecutePaymentResult?>(_options.StateStoreName, ExecuteKey(paymentId), cancellationToken: ct);

    public Task SaveExecuteResultAsync(string paymentId, ExecutePaymentResult result, CancellationToken ct = default)
        => _dapr.SaveStateAsync(_options.StateStoreName, ExecuteKey(paymentId), result, cancellationToken: ct);

    private static string ReserveKey(string paymentId) => ReserveKeyPrefix + paymentId;
    private static string ExecuteKey(string paymentId) => ExecuteKeyPrefix + paymentId;
}
