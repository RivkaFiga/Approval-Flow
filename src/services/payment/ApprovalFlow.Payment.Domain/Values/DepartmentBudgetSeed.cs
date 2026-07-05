namespace ApprovalFlow.Payment.Domain.Values;

/// <summary>
/// A single seed record used to bootstrap a department's initial budget (from configuration mirroring
/// <c>sample-invoices.json → budgets</c>). Living in the domain keeps the infrastructure seeder free of
/// framework types and lets the domain aggregate <see cref="Entities.DepartmentBudget.Initialize"/> validate
/// the input.
/// </summary>
public sealed record DepartmentBudgetSeed(string Department, decimal InitialUsd);
