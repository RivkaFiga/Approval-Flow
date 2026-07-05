using ApprovalFlow.AiDecision.Domain.Values;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Domain.Rules;

/// <summary>
/// Deterministic CategoryRules evaluator (§7 C6/C7). Reads structured invoice fields and returns the
/// category-compliance verdict the router consumes directly — the agent's <c>policy_violations</c> can
/// never flip this outcome (§7.1).
/// </summary>
public static class CategoryRulesEvaluator
{
    private const decimal MealPerAttendeeCapUsd = 75m;
    private const decimal MealClientEntertainmentThresholdUsd = 500m;
    private const decimal SaasMonthlyCapUsd = 200m;
    private const decimal HardwareCapitalThresholdUsd = 1000m;
    private const decimal TravelSingleExpenseThresholdUsd = 1500m;
    private const int Meal02MinNotesLength = 20;

    private static readonly string[] AlcoholKeywords =
    {
        "alcohol", "wine", "beer", "liquor", "spirits", "vodka", "whiskey", "whisky", "cocktail", "champagne"
    };

    private static readonly string[] BusinessClassKeywords =
    {
        "first class", "first-class", "business class", "business-class"
    };

    public static CategoryRuleResult Evaluate(Invoice invoice, decimal amountUsd)
    {
        var rejects = new List<PolicyViolation>();
        var escalates = new List<PolicyViolation>();

        switch (invoice.Category)
        {
            case ExpenseCategory.Meals:
                EvaluateMeals(invoice, amountUsd, rejects, escalates);
                break;
            case ExpenseCategory.Saas:
                EvaluateSaas(amountUsd, escalates);
                break;
            case ExpenseCategory.Hardware:
                EvaluateHardware(amountUsd, escalates);
                break;
            case ExpenseCategory.Travel:
                EvaluateTravel(invoice, amountUsd, escalates);
                break;
        }

        return new CategoryRuleResult
        {
            RejectViolations = rejects,
            EscalateViolations = escalates
        };
    }

    private static void EvaluateMeals(Invoice invoice, decimal amountUsd, List<PolicyViolation> rejects, List<PolicyViolation> escalates)
    {
        if (IsAlcoholOnly(invoice))
        {
            rejects.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.Meal03,
                Detail = "Alcohol-only receipts are not reimbursable."
            });
            return;
        }

        if (invoice.Attendees is null || invoice.Attendees <= 0)
        {
            escalates.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.Meal01,
                Detail = "Missing attendee count for meals."
            });
            return;
        }

        var perAttendee = amountUsd / invoice.Attendees.Value;
        if (perAttendee > MealPerAttendeeCapUsd)
        {
            escalates.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.Meal01,
                Detail = $"Per-attendee ${perAttendee:F2} exceeds $75 cap."
            });
        }

        if (amountUsd > MealClientEntertainmentThresholdUsd && !HasClientJustification(invoice.Notes))
        {
            escalates.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.Meal02,
                Detail = "Client entertainment over $500 requires client name + business justification."
            });
        }
    }

    private static void EvaluateSaas(decimal amountUsd, List<PolicyViolation> escalates)
    {
        if (amountUsd > SaasMonthlyCapUsd)
        {
            escalates.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.Saas01,
                Detail = $"SaaS ${amountUsd:F2} exceeds $200/month cap."
            });
        }
    }

    private static void EvaluateHardware(decimal amountUsd, List<PolicyViolation> escalates)
    {
        if (amountUsd > HardwareCapitalThresholdUsd)
        {
            escalates.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.Hw02,
                Detail = $"Hardware ${amountUsd:F2} is Capital (>$1,000) — always human-approved."
            });
        }
    }

    private static void EvaluateTravel(Invoice invoice, decimal amountUsd, List<PolicyViolation> escalates)
    {
        if (amountUsd > TravelSingleExpenseThresholdUsd)
        {
            escalates.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.Travel02,
                Detail = $"Travel ${amountUsd:F2} exceeds $1,500 — manager approval required."
            });
        }

        if (HasBusinessClass(invoice))
        {
            escalates.Add(new PolicyViolation
            {
                RuleId = PolicyRuleIds.Travel03,
                Detail = "First/business-class travel always requires approval."
            });
        }
    }

    private static bool IsAlcoholOnly(Invoice invoice)
    {
        if (invoice.LineItems.Count == 0) return false;
        return invoice.LineItems.All(li => ContainsAny(li.Description, AlcoholKeywords));
    }

    private static bool HasBusinessClass(Invoice invoice)
        => invoice.LineItems.Any(li => ContainsAny(li.Description, BusinessClassKeywords));

    private static bool HasClientJustification(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return false;
        if (notes.Length < Meal02MinNotesLength) return false;
        var lower = notes.ToLowerInvariant();
        if (lower.Contains("no client name")) return false;
        return lower.Contains("client");
    }

    private static bool ContainsAny(string text, string[] keywords)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var lower = text.ToLowerInvariant();
        return keywords.Any(k => lower.Contains(k));
    }
}
