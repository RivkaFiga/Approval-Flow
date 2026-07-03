namespace ApprovalFlow.Intake.Domain.Entities;

public sealed class SubmittedInvoice
{
    public Guid Id { get; private set; }
    public string TrackingId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string Vendor { get; private set; } = string.Empty;
    public bool VendorKnown { get; private set; }
    public string Submitter { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public int Category { get; private set; }
    public string Currency { get; private set; } = "USD";
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public bool ReceiptPresent { get; private set; }
    public int? Attendees { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Notes { get; private set; }
    public string LineItemsJson { get; private set; } = "[]";
    public string? DedupKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsDuplicate { get; private set; }

    private SubmittedInvoice() { }

    public static SubmittedInvoice Create(
        string trackingId,
        string correlationId,
        string invoiceNumber,
        string vendor,
        bool vendorKnown,
        string submitter,
        string department,
        int category,
        string currency,
        decimal taxAmount,
        decimal total,
        bool receiptPresent,
        int? attendees,
        DateOnly date,
        string? notes,
        string lineItemsJson,
        string dedupKey)
    {
        return new SubmittedInvoice
        {
            Id = Guid.NewGuid(),
            TrackingId = trackingId,
            CorrelationId = correlationId,
            InvoiceNumber = invoiceNumber,
            Vendor = vendor,
            VendorKnown = vendorKnown,
            Submitter = submitter,
            Department = department,
            Category = category,
            Currency = currency,
            TaxAmount = taxAmount,
            Total = total,
            ReceiptPresent = receiptPresent,
            Attendees = attendees,
            Date = date,
            Notes = notes,
            LineItemsJson = lineItemsJson,
            DedupKey = dedupKey,
            CreatedAt = DateTimeOffset.UtcNow,
            IsDuplicate = false
        };
    }

    public void MarkDuplicate() => IsDuplicate = true;
}
