namespace ApprovalFlow.Contracts.Models;

/// <summary>A single line on an invoice/expense.</summary>
public sealed record LineItem
{
    public string Description { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
