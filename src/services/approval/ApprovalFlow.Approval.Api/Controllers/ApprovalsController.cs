using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Invocation.V1;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Approval.Api.Controllers;

/// <summary>
/// HITL resume endpoints (F5, §9). Each action raises a single <c>ApprovalDecision</c> external event on
/// the durable workflow instance identified by <paramref name="trackingId"/>:
/// <list type="bullet">
///   <item><c>POST /approvals/{trackingId}/approve</c> — approver approves; workflow finalizes as
///     <c>paid</c> (human path).</item>
///   <item><c>POST /approvals/{trackingId}/reject</c> — approver rejects; workflow finalizes as
///     <c>rejected</c>.</item>
///   <item><c>POST /approvals/{trackingId}/request-info</c> — approver sends the item back for missing
///     info; workflow publishes <c>review.status(awaiting-info)</c> and durably re-pauses on the same
///     instance (§9, <c>trackingId</c> resume contract).</item>
/// </list>
/// </summary>
[ApiController]
[Route("approvals")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalWorkflowEventRaiser _raiser;
    private readonly ILogger<ApprovalsController> _logger;

    public ApprovalsController(
        IApprovalWorkflowEventRaiser raiser,
        ILogger<ApprovalsController> logger)
    {
        _raiser = raiser;
        _logger = logger;
    }

    [HttpPost("{trackingId}/approve")]
    public Task<IActionResult> Approve(string trackingId, [FromBody] ApproverActionInput body, CancellationToken ct)
        => RaiseAsync(trackingId, ApproverActionType.Approve, body, ct);

    [HttpPost("{trackingId}/reject")]
    public Task<IActionResult> Reject(string trackingId, [FromBody] ApproverActionInput body, CancellationToken ct)
        => RaiseAsync(trackingId, ApproverActionType.Reject, body, ct);

    [HttpPost("{trackingId}/request-info")]
    public Task<IActionResult> RequestInfo(string trackingId, [FromBody] ApproverActionInput body, CancellationToken ct)
        => RaiseAsync(trackingId, ApproverActionType.SendBack, body, ct);

    private async Task<IActionResult> RaiseAsync(
        string trackingId,
        ApproverActionType action,
        ApproverActionInput body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(trackingId))
        {
            return BadRequest(new { error = "trackingId is required." });
        }
        if (string.IsNullOrWhiteSpace(body.ApproverId))
        {
            return BadRequest(new { error = "approverId is required." });
        }

        using (LogContext.PushProperty("TrackingId", trackingId))
        using (LogContext.PushProperty("ApproverId", body.ApproverId))
        {
            _logger.LogInformation("Raising ApprovalDecision {Action} for TrackingId {TrackingId}.",
                action, trackingId);

            await _raiser.RaiseApprovalDecisionAsync(
                trackingId,
                new ApproverDecisionPayload
                {
                    Action = action,
                    ApproverId = body.ApproverId,
                    Comment = body.Comment
                },
                ct);
        }

        return Accepted(new ApproverActionResponse
        {
            TrackingId = trackingId,
            Status = MapStatus(action)
        });
    }

    private static LifecycleStatus MapStatus(ApproverActionType action) => action switch
    {
        ApproverActionType.Approve => LifecycleStatus.Paying,
        ApproverActionType.Reject => LifecycleStatus.Rejected,
        ApproverActionType.SendBack => LifecycleStatus.AwaitingInfo,
        _ => LifecycleStatus.AwaitingApproval
    };

    /// <summary>
    /// HITL request body. The action verb is fixed by the route; only the approver identity and an
    /// optional comment (reject reason or "what we still need" note) are supplied per call.
    /// </summary>
    public sealed record ApproverActionInput
    {
        public string ApproverId { get; init; } = string.Empty;
        public string? Comment { get; init; }
    }
}
