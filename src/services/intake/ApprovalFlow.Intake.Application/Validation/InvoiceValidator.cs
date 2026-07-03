using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.Intake.Application.Validation;

public static class InvoiceValidator
{
    public static List<string> Validate(Invoice invoice)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            errors.Add("InvoiceNumber is required.");

        if (string.IsNullOrWhiteSpace(invoice.Vendor))
            errors.Add("Vendor is required.");

        if (string.IsNullOrWhiteSpace(invoice.Submitter))
            errors.Add("Submitter is required.");

        if (string.IsNullOrWhiteSpace(invoice.Department))
            errors.Add("Department is required.");

        if (string.IsNullOrWhiteSpace(invoice.Currency) || invoice.Currency.Length != 3)
            errors.Add("Currency must be a 3-letter ISO code.");

        if (invoice.Total <= 0)
            errors.Add("Total must be greater than zero.");

        if (invoice.TaxAmount < 0)
            errors.Add("TaxAmount cannot be negative.");

        if (invoice.LineItems.Count == 0)
            errors.Add("At least one LineItem is required.");

        foreach (var item in invoice.LineItems)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
                errors.Add("LineItem Description is required.");

            if (item.Quantity <= 0)
                errors.Add($"LineItem '{item.Description}' Quantity must be positive.");

            if (item.UnitPrice <= 0)
                errors.Add($"LineItem '{item.Description}' UnitPrice must be positive.");
        }

        if (invoice.Date == default)
            errors.Add("Date is required.");

        if (invoice.Category == Contracts.Enums.ExpenseCategory.Meals && (invoice.Attendees == null || invoice.Attendees <= 0))
            errors.Add("Attendees count is required for Meals category.");

        return errors;
    }
}
