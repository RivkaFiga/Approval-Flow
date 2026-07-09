using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Gateway.Auth;
using Dapr.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.Gateway.Controllers;

[ApiController]
[Route("approvals")]
[Authorize(Policy = "Approver")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly DaprClient _dapr;

    public ApprovalsController(DaprClient dapr) => _dapr = dapr;

    [HttpGet("queue")]
    [ProducesResponseType(typeof(ApproverQueueResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Queue(CancellationToken ct)
    {
        var response = await _dapr.InvokeMethodAsync<ApproverQueueResponse>(
            HttpMethod.Get, "approval", "approvals/queue", ct);
        return Ok(response);
    }

    [HttpPost("{trackingId}/approve")]
    [ProducesResponseType(typeof(ApproverActionResponse), StatusCodes.Status202Accepted)]
    public Task<IActionResult> Approve(string trackingId, [FromBody] ApproverInput body, CancellationToken ct)
        => InvokeActionAsync(trackingId, "approve", body, ct);

    [HttpPost("{trackingId}/reject")]
    [ProducesResponseType(typeof(ApproverActionResponse), StatusCodes.Status202Accepted)]
    public Task<IActionResult> Reject(string trackingId, [FromBody] ApproverInput body, CancellationToken ct)
        => InvokeActionAsync(trackingId, "reject", body, ct);

    [HttpPost("{trackingId}/request-info")]
    [ProducesResponseType(typeof(ApproverActionResponse), StatusCodes.Status202Accepted)]
    public Task<IActionResult> RequestInfo(string trackingId, [FromBody] ApproverInput body, CancellationToken ct)
        => InvokeActionAsync(trackingId, "request-info", body, ct);

    private async Task<IActionResult> InvokeActionAsync(
        string trackingId, string action, ApproverInput body, CancellationToken ct)
    {
        var approverId = ApproverIdentity.TryResolve(User);
        if (approverId is null)
            return Unauthorized();

        var authoritative = body with { ApproverId = approverId };

        var response = await _dapr.InvokeMethodAsync<ApproverInput, ApproverActionResponse>(
            HttpMethod.Post, "approval", $"approvals/{trackingId}/{action}", authoritative, ct);
        return Accepted(response);
    }

    /// <summary>Body accepted by each approver-action route; mirrors the Approval service contract.</summary>
    public sealed record ApproverInput
    {
        public string ApproverId { get; init; } = string.Empty;
        public string? Comment { get; init; }
    }
}
