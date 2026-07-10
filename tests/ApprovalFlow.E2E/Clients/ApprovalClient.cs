using System.Net.Http.Json;
using System.Text.Json;
using ApprovalFlow.Contracts.Invocation.V1;

namespace ApprovalFlow.E2E.Clients;

internal sealed class ApprovalClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ApprovalClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<ApproverQueueResponse?> GetQueueAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync("/approvals/queue", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApproverQueueResponse>(_json, ct);
    }

    public async Task ApproveAsync(string trackingId, string approverId, string? comment, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync(
            $"/approvals/{trackingId}/approve",
            new { approverId, comment },
            _json,
            ct);
        response.EnsureSuccessStatusCode();
    }
}
