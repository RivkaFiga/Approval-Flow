using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Models;
using ApprovalFlow.Intake.Application.Validation;
using Xunit;

namespace ApprovalFlow.Intake.Tests;

public class InvoiceValidatorTests
{
    private static Invoice ValidInvoice() => new()
    {
        InvoiceNumber = "INV-001",
        Vendor = "Acme Corp",
        Submitter = "jane@example.com",
        Department = "Engineering",
        Category = ExpenseCategory.Saas,
        Currency = "USD",
        Total = 500m,
        TaxAmount = 50m,
        ReceiptPresent = true,
        Date = new DateOnly(2026, 7, 1),
        LineItems = new[]
        {
            new LineItem { Description = "License", Quantity = 1, UnitPrice = 500m }
        }
    };

    [Fact]
    public void Valid_invoice_produces_no_errors()
    {
        var errors = InvoiceValidator.Validate(ValidInvoice());
        Assert.Empty(errors);
    }

    [Fact]
    public void Missing_invoice_number_produces_error()
    {
        var inv = ValidInvoice() with { InvoiceNumber = "" };
        var errors = InvoiceValidator.Validate(inv);
        Assert.Contains(errors, e => e.Contains("InvoiceNumber"));
    }

    [Fact]
    public void Missing_vendor_produces_error()
    {
        var inv = ValidInvoice() with { Vendor = "" };
        var errors = InvoiceValidator.Validate(inv);
        Assert.Contains(errors, e => e.Contains("Vendor"));
    }

    [Fact]
    public void Zero_total_produces_error()
    {
        var inv = ValidInvoice() with { Total = 0 };
        var errors = InvoiceValidator.Validate(inv);
        Assert.Contains(errors, e => e.Contains("Total"));
    }

    [Fact]
    public void No_line_items_produces_error()
    {
        var inv = ValidInvoice() with { LineItems = Array.Empty<LineItem>() };
        var errors = InvoiceValidator.Validate(inv);
        Assert.Contains(errors, e => e.Contains("LineItem"));
    }

    [Fact]
    public void Meals_without_attendees_produces_error()
    {
        var inv = ValidInvoice() with { Category = ExpenseCategory.Meals, Attendees = null };
        var errors = InvoiceValidator.Validate(inv);
        Assert.Contains(errors, e => e.Contains("Attendees"));
    }

    [Fact]
    public void Meals_with_attendees_is_valid()
    {
        var inv = ValidInvoice() with { Category = ExpenseCategory.Meals, Attendees = 3 };
        var errors = InvoiceValidator.Validate(inv);
        Assert.Empty(errors);
    }

    [Fact]
    public void Invalid_currency_length_produces_error()
    {
        var inv = ValidInvoice() with { Currency = "US" };
        var errors = InvoiceValidator.Validate(inv);
        Assert.Contains(errors, e => e.Contains("Currency"));
    }

    [Fact]
    public void Negative_tax_produces_error()
    {
        var inv = ValidInvoice() with { TaxAmount = -10m };
        var errors = InvoiceValidator.Validate(inv);
        Assert.Contains(errors, e => e.Contains("TaxAmount"));
    }
}
