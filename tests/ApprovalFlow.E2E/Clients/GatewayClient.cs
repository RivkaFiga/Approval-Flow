using System.Net.Http.Json;
using System.Text.Json;
using ApprovalFlow.Contracts.Invocation.V1;

namespace ApprovalFlow.E2E.Clients;

internal sealed class GatewayClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public GatewayClient(string baseUrl)
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

    public async Task<SubmitInvoiceResponse> SubmitAsync(SubmitInvoiceRequest request, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("/api/intake", request, _json, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SubmitInvoiceResponse>(_json, ct);
        return result ?? throw new InvalidOperationException("Empty response body from POST /api/intake.");
    }
}
