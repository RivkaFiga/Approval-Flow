using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.Contracts.Invocation.V1;

/// <summary>Gateway → Intake: submit an invoice/expense for async processing (§5.1, F1).</summary>
public sealed record SubmitInvoiceRequest
{
    public Invoice Invoice { get; init; } = new();

    /// <summary>
    /// Optional reference to an open item being corrected (<c>X-Corrects-Tracking-Id</c>, §9). When set,
    /// Intake routes the POST to the open workflow as <c>InfoProvided</c> instead of running fresh dedup.
    /// </summary>
    public string? CorrectsTrackingId { get; init; }
}

/// <summary>Intake → Gateway: the <c>202 Accepted</c> body (§6). Intake never blocks on processing.</summary>
public sealed record SubmitInvoiceResponse
{
    public string TrackingId { get; init; } = string.Empty;

    /// <summary><c>Accepted</c> for a new item, or <c>Duplicate</c> for a re-submission (GLOBAL-DUP).</summary>
    public AcceptanceStatus Status { get; init; }
}
