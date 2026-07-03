using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.Contracts.Events.V1;

/// <summary>
/// <c>invoice.submitted</c> — published by Intake, consumed by AI-Decision and Notification (§5.2).
/// Notification records status <c>received</c>.
/// </summary>
public sealed record InvoiceSubmittedV1 : IntegrationEvent
{
    public override string Type => EventTypes.InvoiceSubmitted;
    public override int SchemaVersion => 1;

    /// <summary>Submitter-facing opaque handle returned by the 202 (F1); polled for status (F2).</summary>
    public string TrackingId { get; init; } = string.Empty;

    public Invoice Invoice { get; init; } = new();
}
