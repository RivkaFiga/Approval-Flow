using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ApprovalFlow.Payment.Api.Controllers;

/// <summary>
/// Payment saga HTTP surface (§8). Invoked by Approval/Workflow via Dapr service invocation. This service
/// is intentionally synchronous — per §5.1/§5.2 the Workflow is the sole publisher of <c>item.finalized</c>,
/// so Payment returns its outcome as an HTTP response and does not emit its own topic (no
/// <c>payment-completed</c> event exists in <c>EventTypes</c> for that reason).
/// </summary>
[ApiController]
[Route("payments")]
public sealed class PaymentController : ControllerBase
{
    private readonly ReserveBudgetService _reserve;
    private readonly ExecutePaymentService _execute;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        ReserveBudgetService reserve,
        ExecutePaymentService execute,
        ILogger<PaymentController> logger)
    {
        _reserve = reserve;
        _execute = execute;
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

    /// <summary>
    /// Charge the payment provider against a previously reserved budget and append the ledger row
    /// (saga step 2, §8). Idempotent on <c>paymentId</c> — retries produce the same result (§10).
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<ExecutePaymentResult>> Execute(
        [FromBody] ExecutePaymentRequest request,
        CancellationToken ct)
    {
        using (LogContext.PushProperty("CorrelationId", request.CorrelationId))
        using (LogContext.PushProperty("TrackingId", request.TrackingId))
        using (LogContext.PushProperty("PaymentId", request.PaymentId))
        {
            _logger.LogInformation(
                "Execute request for Department {Department}, Amount {Amount} USD.",
                request.Department, request.AmountUsd);
            var result = await _execute.HandleAsync(request, ct);
            return Ok(result);
        }
    }
}
