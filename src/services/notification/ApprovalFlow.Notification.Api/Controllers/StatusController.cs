using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Notification.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.Notification.Api.Controllers;

/// <summary>
/// F2 read surface: <c>GET /api/status/{trackingId}</c> returns the live projection built from the
/// lifecycle events in §5.2 (not just the terminal outcome). 404 when the tracking id is unknown.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class StatusController : ControllerBase
{
    private readonly GetSubmissionStatusService _service;

    public StatusController(GetSubmissionStatusService service) => _service = service;

    [HttpGet("{trackingId}")]
    [ProducesResponseType(typeof(SubmissionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string trackingId, CancellationToken ct)
    {
        var response = await _service.GetAsync(trackingId, ct);
        if (response is null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Submission not found.",
                detail: $"No submission tracked for '{trackingId}'.");

        return Ok(response);
    }
}
