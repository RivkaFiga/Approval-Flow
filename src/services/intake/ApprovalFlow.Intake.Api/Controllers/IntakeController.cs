using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Intake.Application.Services;
using ApprovalFlow.Intake.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.Intake.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class IntakeController : ControllerBase
{
    private readonly IntakeService _service;

    public IntakeController(IntakeService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(SubmitInvoiceResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] SubmitInvoiceRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N");

        try
        {
            var response = await _service.SubmitAsync(request, correlationId, ct);
            return Accepted(response);
        }
        catch (InvoiceValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation failed.",
                detail: string.Join("; ", ex.Errors));
        }
    }
}
