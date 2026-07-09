using ApprovalFlow.AiDecision.Application.Ports;
using ApprovalFlow.Contracts.Invocation.V1;
using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.AiDecision.Infrastructure.Policy;

/// <summary>
/// Reads the active policy snapshot from Config/Policy via Dapr service invocation and caches it in-process
/// until a <c>policy.changed</c> event calls <see cref="Invalidate"/> (§5.3a). If Config/Policy is unavailable
/// the appsettings-backed <see cref="ConfigPolicySnapshotProvider"/> is used so decisions keep flowing.
/// </summary>
public sealed class DaprConfigPolicySnapshotProvider : IPolicySnapshotProvider
{
    private const string ConfigPolicyAppId = "config-policy";
    private const string SnapshotMethod = "api/policy-snapshot";

    private readonly DaprClient _dapr;
    private readonly ConfigPolicySnapshotProvider _fallback;
    private readonly ILogger<DaprConfigPolicySnapshotProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private PolicySnapshotResponse? _cached;

    public DaprConfigPolicySnapshotProvider(
        DaprClient dapr,
        ConfigPolicySnapshotProvider fallback,
        ILogger<DaprConfigPolicySnapshotProvider> logger)
    {
        _dapr = dapr;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<PolicySnapshotResponse> GetAsync(CancellationToken ct = default)
    {
        var snapshot = _cached;
        if (snapshot is not null)
            return snapshot;

        await _gate.WaitAsync(ct);
        try
        {
            if (_cached is not null)
                return _cached;

            try
            {
                var fetched = await _dapr.InvokeMethodAsync<PolicySnapshotResponse>(
                    HttpMethod.Get, ConfigPolicyAppId, SnapshotMethod, ct);

                _cached = fetched;
                _logger.LogInformation(
                    "Loaded policy snapshot from Config/Policy (version {Version}).", fetched.Version);
                return fetched;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Config/Policy snapshot fetch failed; serving fallback from local configuration.");
                return await _fallback.GetAsync(ct);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate()
    {
        _cached = null;
        _logger.LogInformation("Policy snapshot cache invalidated by policy.changed event.");
    }
}
