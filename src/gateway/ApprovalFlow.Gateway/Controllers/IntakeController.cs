using ApprovalFlow.Contracts.Invocation.V1;
using Dapr.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Submitter")]
public sealed class IntakeController : ControllerBase
{
    private readonly DaprClient _dapr;

    public IntakeController(DaprClient dapr) => _dapr = dapr;

    [HttpPost]
    [ProducesResponseType(typeof(SubmitInvoiceResponse), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Submit([FromBody] SubmitInvoiceRequest request, CancellationToken ct)
    {
        var response = await _dapr.InvokeMethodAsync<SubmitInvoiceRequest, SubmitInvoiceResponse>(
            HttpMethod.Post, "intake", "api/Intake", request, ct);

        return Accepted(response);
    }
}
