using System.Text.Json;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.E2E.Helpers;

internal static class FixtureLoader
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<SampleFixture> Load(string path)
    {
        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<SampleInvoiceFile>(json, _json)
                   ?? throw new InvalidOperationException("Failed to parse sample-invoices.json.");
        return file.Fixtures;
    }

    public static SubmitInvoiceRequest ToRequest(SampleFixture f) => new()
    {
        Invoice = new Invoice
        {
            // Append a short random suffix so each test run produces a unique dedup key and
            // intake never short-circuits with AcceptanceStatus.Duplicate on repeated runs.
            InvoiceNumber  = $"{f.InvoiceNumber}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Vendor         = f.Vendor,
            VendorKnown    = f.VendorKnown,
            Submitter      = f.Submitter,
            Department     = f.Department,
            Category       = ParseCategory(f.Category),
            Currency       = f.Currency,
            LineItems      = f.LineItems.Select(li => new LineItem
                             {
                                 Description = li.Description,
                                 Quantity    = li.Quantity,
                                 UnitPrice   = li.UnitPrice
                             }).ToList(),
            TaxAmount      = f.TaxAmount,
            Total          = f.Total,
            ReceiptPresent = f.ReceiptPresent,
            Attendees      = f.Attendees,
            Date           = DateOnly.Parse(f.Date),
            Notes          = f.Notes
        }
    };

    private static ExpenseCategory ParseCategory(string s) => s switch
    {
        "meals"    => ExpenseCategory.Meals,
        "saas"     => ExpenseCategory.Saas,
        "hardware" => ExpenseCategory.Hardware,
        "travel"   => ExpenseCategory.Travel,
        _          => ExpenseCategory.Other
    };
}

internal sealed class SampleInvoiceFile
{
    public List<SampleFixture> Fixtures { get; init; } = [];
}

internal sealed class SampleFixture
{
    public string  Id             { get; init; } = string.Empty;
    public string  Submitter      { get; init; } = string.Empty;
    public string  Department     { get; init; } = string.Empty;
    public string  Vendor         { get; init; } = string.Empty;
    public bool    VendorKnown    { get; init; }
    public string  InvoiceNumber  { get; init; } = string.Empty;
    public string  Currency       { get; init; } = "USD";
    public string  Category       { get; init; } = string.Empty;
    public int?    Attendees      { get; init; }
    public List<SampleLineItem> LineItems { get; init; } = [];
    public decimal TaxAmount      { get; init; }
    public decimal Total          { get; init; }
    public bool    ReceiptPresent { get; init; }
    public string  Date           { get; init; } = string.Empty;
    public string? Notes          { get; init; }
    public SampleExpected Expected { get; init; } = new();
}

internal sealed class SampleLineItem
{
    public string  Description { get; init; } = string.Empty;
    public int     Quantity    { get; init; }
    public decimal UnitPrice   { get; init; }
}

internal sealed class SampleExpected
{
    public string Route { get; init; } = string.Empty;
}
