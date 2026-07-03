using ApprovalFlow.ConfigPolicy.Application.Dtos;
using ApprovalFlow.ConfigPolicy.Application.Services;
using ApprovalFlow.ConfigPolicy.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ApprovalFlow.ConfigPolicy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PoliciesController : ControllerBase
{
    private readonly PolicyService _service;

    public PoliciesController(PolicyService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(PolicyDocumentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePolicyRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var dto = await _service.GetByIdAsync(id, ct);
            return Ok(dto);
        }
        catch (PolicyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PolicyDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        return Ok(list);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PolicyDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePolicyRequest request, CancellationToken ct)
    {
        try
        {
            var dto = await _service.UpdateAsync(id, request, ct);
            return Ok(dto);
        }
        catch (PolicyNotFoundException)
        {
            return NotFound();
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (PolicyNotFoundException)
        {
            return NotFound();
        }
    }
}
