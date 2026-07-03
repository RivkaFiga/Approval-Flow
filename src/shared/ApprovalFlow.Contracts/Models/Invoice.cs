using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Contracts.Models;

/// <summary>
/// Normalized invoice/expense as carried across service boundaries — the submission body and the
/// <c>invoice.submitted</c> payload. Structured data only; no OCR (§1.4). Amounts are in <see cref="Currency"/>
/// and converted to USD at the submission-date rate before rules apply (GLOBAL-FX, §7).
/// </summary>
public sealed record Invoice
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public string Vendor { get; init; } = string.Empty;
    /// <summary>
    /// Submitter-declared vendor familiarity. This is <em>not</em> authoritative for the
    /// <c>GLOBAL-VENDOR</c> hard-stop; AI-Decision re-verifies independently against
    /// Config/Policy's known-vendor list (§7, C4).
    /// </summary>
    public bool VendorKnown { get; init; }
    public string Submitter { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public ExpenseCategory Category { get; init; }

    /// <summary>ISO currency code of the amounts (e.g. <c>USD</c>, <c>EUR</c>).</summary>
    public string Currency { get; init; } = "USD";

    public IReadOnlyList<LineItem> LineItems { get; init; } = Array.Empty<LineItem>();
    public decimal TaxAmount { get; init; }
    public decimal Total { get; init; }
    public bool ReceiptPresent { get; init; }

    /// <summary>Attendee count for meals (MEAL-01 required-field check); null when not applicable.</summary>
    public int? Attendees { get; init; }

    /// <summary>Submission date; the rate date used for FX conversion (§7).</summary>
    public DateOnly Date { get; init; }

    public string? Notes { get; init; }
}
