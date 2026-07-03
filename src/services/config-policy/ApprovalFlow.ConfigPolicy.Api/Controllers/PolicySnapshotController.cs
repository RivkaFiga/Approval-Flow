using ApprovalFlow.ConfigPolicy.Application.Services;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.ConfigPolicy.Api.Controllers;

[ApiController]
[Route("api/policy-snapshot")]
public sealed class PolicySnapshotController : ControllerBase
{
    private readonly PolicyService _service;

    public PolicySnapshotController(PolicyService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(PolicySnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSnapshot(CancellationToken ct)
    {
        try
        {
            var dto = await _service.GetActiveSnapshotAsync(ct);
            var response = new PolicySnapshotResponse
            {
                Version = dto.Version.ToString(),
                Thresholds = new AutonomyThresholds
                {
                    CeilingUsd = dto.AutonomyCeilingUsd,
                    MinConfidence = dto.AutonomyMinConfidence
                },
                BaseCurrency = dto.BaseCurrency,
                FxRates = dto.FxRates,
                KnownVendors = dto.KnownVendors,
                PolicyMarkdown = dto.Markdown
            };
            return Ok(response);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { error = "No active policy document exists." });
        }
    }
}
