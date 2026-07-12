using System.Net.Http.Json;
using System.Text.Json;

namespace ApprovalFlow.E2E.Clients;

/// <summary>
/// Direct client for the Config/Policy service used by hot-reload E2E tests. The service is exposed on a
/// host port only for E2E (docker-compose maps 5108 → 8080); production traffic stays inside the mesh via
/// Dapr service invocation.
/// </summary>
internal sealed class ConfigPolicyClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ConfigPolicyClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync("/healthz", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PolicyDocumentSummary> CreateAsync(CreatePolicyBody body, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("/api/policies", body, _json, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PolicyDocumentSummary>(_json, ct)
               ?? throw new InvalidOperationException("Empty body from POST /api/policies.");
    }

    public async Task<PolicySnapshotResponseBody> GetActiveSnapshotAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync("/api/policy-snapshot", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PolicySnapshotResponseBody>(_json, ct)
               ?? throw new InvalidOperationException("Empty body from GET /api/policy-snapshot.");
    }
}

internal sealed record CreatePolicyBody(
    string Name,
    string Markdown,
    decimal AutonomyCeilingUsd,
    double AutonomyMinConfidence,
    string BaseCurrency,
    Dictionary<string, decimal> FxRates,
    List<string> KnownVendors);

internal sealed record PolicyDocumentSummary
{
    public Guid Id { get; init; }
    public int Version { get; init; }
    public decimal AutonomyCeilingUsd { get; init; }
    public double AutonomyMinConfidence { get; init; }
}

internal sealed record PolicySnapshotResponseBody
{
    public string Version { get; init; } = string.Empty;
    public AutonomyThresholdsBody Thresholds { get; init; } = new();
}

internal sealed record AutonomyThresholdsBody
{
    public decimal CeilingUsd { get; init; }
    public double MinConfidence { get; init; }
}
