using ApprovalFlow.AiDecision.Application.Ports;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.AiDecision.Infrastructure.Policy;

/// <summary>
/// Configuration-backed policy snapshot. Reads thresholds, FX rates and known vendors from appsettings so
/// the vertical slice runs standalone; the same port will later be adapted to Dapr service invocation
/// against Config/Policy (§5.1) without any Application/Domain change (M15).
/// </summary>
public sealed class ConfigPolicySnapshotProvider : IPolicySnapshotProvider
{
    private readonly IOptionsMonitor<PolicySnapshotOptions> _options;

    public ConfigPolicySnapshotProvider(IOptionsMonitor<PolicySnapshotOptions> options) => _options = options;

    public Task<PolicySnapshotResponse> GetAsync(CancellationToken ct = default)
    {
        var opts = _options.CurrentValue;

        var response = new PolicySnapshotResponse
        {
            Version = opts.Version,
            BaseCurrency = opts.BaseCurrency,
            Thresholds = new AutonomyThresholds
            {
                CeilingUsd = opts.CeilingUsd,
                MinConfidence = opts.MinConfidence
            },
            FxRates = opts.FxRates,
            KnownVendors = opts.KnownVendors
        };

        return Task.FromResult(response);
    }
}
