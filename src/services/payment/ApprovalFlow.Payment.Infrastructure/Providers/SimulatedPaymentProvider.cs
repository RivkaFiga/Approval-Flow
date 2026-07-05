using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Domain.Values;
using ApprovalFlow.Payment.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Payment.Infrastructure.Providers;

/// <summary>
/// Stand-in for a real payment rail (§1.4). Deterministic: succeeds by default and synthesizes an opaque
/// provider reference; forces a failure when the charge matches any configured id in
/// <see cref="SimulatedPaymentProviderOptions"/>. The forced-failure mechanism is what makes INV-1012
/// reproducible without a code change (§12.3, M13).
/// </summary>
public sealed class SimulatedPaymentProvider : IPaymentProvider
{
    private readonly SimulatedPaymentProviderOptions _options;

    public SimulatedPaymentProvider(IOptions<SimulatedPaymentProviderOptions> options)
    {
        _options = options.Value;
    }

    public Task<ChargeResult> ChargeAsync(ChargeCommand command, CancellationToken ct = default)
    {
        if (ShouldForceFailure(command))
            return Task.FromResult(ChargeResult.Failure(_options.FailureReason));

        // Include the paymentId in the reference so the ledger row can be traced back to the caller's key
        // during audit, without exposing internal state (§12.1/F9).
        var reference = $"SIM-{command.PaymentId}";
        return Task.FromResult(ChargeResult.Success(reference));
    }

    private bool ShouldForceFailure(ChargeCommand command)
        => _options.FailPaymentIds.Contains(command.PaymentId, StringComparer.Ordinal)
        || _options.FailTrackingIds.Contains(command.TrackingId, StringComparer.Ordinal);
}
