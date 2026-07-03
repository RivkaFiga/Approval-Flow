namespace ApprovalFlow.Contracts.Events;

/// <summary>
/// Canonical CloudEvent <c>type</c> identifiers for the platform's integration events (§5.2).
/// A breaking payload change ships as a new type version alongside a new schema version.
/// </summary>
public static class EventTypes
{
    public const string InvoiceSubmitted = "invoice.submitted";
    public const string DecisionMade = "decision.made";
    public const string ReviewStatus = "review.status";
    public const string ItemFinalized = "item.finalized";
}
