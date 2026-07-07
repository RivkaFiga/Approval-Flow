using System.Net;
using ApprovalFlow.Contracts.Invocation.V1;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatusController : ControllerBase
{
    private readonly DaprClient _dapr;

    public StatusController(DaprClient dapr) => _dapr = dapr;

    [HttpGet("{trackingId}")]
    [ProducesResponseType(typeof(SubmissionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string trackingId, CancellationToken ct)
    {
        var req = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, "notification", $"api/status/{trackingId}");
        using var resp = await _dapr.InvokeMethodWithResponseAsync(req, ct);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return NotFound();

        resp.EnsureSuccessStatusCode();
        return Ok(await resp.Content.ReadFromJsonAsync<SubmissionStatusResponse>(cancellationToken: ct));
    }
}
