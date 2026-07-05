using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Tests;

/// <summary>
/// Test fixtures mirroring the shipped <c>sample-invoices.json</c> and <c>policy.md</c> defaults so the
/// router assertions read like the §7.3 truth table.
/// </summary>
internal static class Fixtures
{
    public static PolicySnapshotResponse DefaultPolicy() => new()
    {
        Version = "policy-v1",
        BaseCurrency = "USD",
        Thresholds = new AutonomyThresholds { CeilingUsd = 250m, MinConfidence = 0.80 },
        FxRates = new Dictionary<string, decimal> { ["EUR"] = 1.08m, ["GBP"] = 1.27m },
        KnownVendors = new[]
        {
            "Bistro 19", "Atlassian", "The Rooftop Grill", "Dell", "Trattoria Verde"
        }
    };

    public static Invoice Meals(
        decimal total,
        int attendees,
        bool receipt = true,
        string vendor = "Bistro 19",
        string? notes = null,
        string currency = "USD",
        IReadOnlyList<LineItem>? lineItems = null,
        decimal tax = 0m)
    {
        var items = lineItems ?? new[] { new LineItem { Description = "Meal", Quantity = 1, UnitPrice = total - tax } };
        return new Invoice
        {
            InvoiceNumber = "NW-INV-TEST",
            Vendor = vendor,
            VendorKnown = true,
            Submitter = "user@northwind.example",
            Department = "engineering-2026Q2",
            Category = ExpenseCategory.Meals,
            Currency = currency,
            LineItems = items,
            TaxAmount = tax,
            Total = total,
            ReceiptPresent = receipt,
            Attendees = attendees,
            Date = new DateOnly(2026, 5, 12),
            Notes = notes
        };
    }

    public static Invoice Saas(decimal total, string vendor = "Atlassian") => new()
    {
        InvoiceNumber = "NW-INV-TEST",
        Vendor = vendor,
        VendorKnown = true,
        Submitter = "user@northwind.example",
        Department = "engineering-2026Q2",
        Category = ExpenseCategory.Saas,
        Currency = "USD",
        LineItems = new[] { new LineItem { Description = "Subscription", Quantity = 1, UnitPrice = total } },
        TaxAmount = 0m,
        Total = total,
        ReceiptPresent = true,
        Date = new DateOnly(2026, 5, 12)
    };

    public static Invoice Hardware(decimal total, string vendor = "Dell") => new()
    {
        InvoiceNumber = "NW-INV-TEST",
        Vendor = vendor,
        VendorKnown = true,
        Submitter = "user@northwind.example",
        Department = "engineering-2026Q2",
        Category = ExpenseCategory.Hardware,
        Currency = "USD",
        LineItems = new[] { new LineItem { Description = "Laptop", Quantity = 1, UnitPrice = total } },
        TaxAmount = 0m,
        Total = total,
        ReceiptPresent = true,
        Date = new DateOnly(2026, 5, 13)
    };

    public static Invoice Travel(decimal total, string vendor = "Bistro 19", string description = "Economy flight") => new()
    {
        InvoiceNumber = "NW-INV-TEST",
        Vendor = vendor,
        VendorKnown = true,
        Submitter = "user@northwind.example",
        Department = "sales-2026Q2",
        Category = ExpenseCategory.Travel,
        Currency = "USD",
        LineItems = new[] { new LineItem { Description = description, Quantity = 1, UnitPrice = total } },
        TaxAmount = 0m,
        Total = total,
        ReceiptPresent = true,
        Date = new DateOnly(2026, 5, 12)
    };
}
