using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApprovalFlow.Contracts.Invocation.V1;

namespace ApprovalFlow.E2E.Clients;

internal sealed class NotificationClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public NotificationClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<SubmissionStatusResponse?> GetStatusAsync(string trackingId, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/status/{trackingId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SubmissionStatusResponse>(_json, ct);
    }
}
