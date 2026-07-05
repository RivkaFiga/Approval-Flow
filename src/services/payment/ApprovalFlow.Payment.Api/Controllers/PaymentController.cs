using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Payment.Api.Controllers;

/// <summary>
/// Payment saga HTTP surface (§8). Invoked by Approval/Workflow via Dapr service invocation. This slice
/// ships the <c>reserve</c> step — the "no overspend" invariant (G4/M9). <c>execute</c> and <c>release</c>
/// land in follow-up slices per the vertical-slice cadence.
/// </summary>
[ApiController]
[Route("payments")]
public sealed class PaymentController : ControllerBase
{
    private readonly ReserveBudgetService _reserve;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        ReserveBudgetService reserve,
        ILogger<PaymentController> logger)
    {
        _reserve = reserve;
        _logger = logger;
    }

    /// <summary>Reserve department budget for one approved item (saga step 1, §8).</summary>
    [HttpPost("reserve")]
    public async Task<ActionResult<ReserveBudgetResult>> Reserve(
        [FromBody] ReserveBudgetRequest request,
        CancellationToken ct)
    {
        // Stitch the caller's correlationId onto every log line so the payment leg joins the item's
        // end-to-end trace (§12.1). The CorrelationId middleware issues a fresh id for this HTTP request;
        // the caller's id takes precedence when present.
        using (LogContext.PushProperty("CorrelationId", request.CorrelationId))
        using (LogContext.PushProperty("TrackingId", request.TrackingId))
        using (LogContext.PushProperty("PaymentId", request.PaymentId))
        {
            _logger.LogInformation(
                "Reserve request for Department {Department}, Amount {Amount} USD.",
                request.Department, request.AmountUsd);
            var result = await _reserve.HandleAsync(request, ct);
            return Ok(result);
        }
    }
}
