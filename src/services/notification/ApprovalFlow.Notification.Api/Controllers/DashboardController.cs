using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Notification.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.Notification.Api.Controllers;

/// <summary>
/// F8 controller dashboard: <c>GET /api/dashboard/summary</c> returns a real-time aggregate
/// of throughput, approval routing, and money split derived from the notification projection store.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly GetDashboardSummaryService _service;

    public DashboardController(GetDashboardSummaryService service) => _service = service;

    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await _service.GetAsync(ct);
        return Ok(summary);
    }
}
