namespace ApprovalFlow.Contracts.Enums;

/// <summary>Expense category of a submitted item. Canonical wire form is lower-case (e.g. <c>meals</c>, <c>saas</c>).</summary>
public enum ExpenseCategory
{
    Meals,
    Saas,
    Hardware,
    Travel,
    Other
}
