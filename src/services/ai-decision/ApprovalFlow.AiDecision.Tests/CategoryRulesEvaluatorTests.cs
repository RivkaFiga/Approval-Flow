using ApprovalFlow.AiDecision.Domain.Rules;
using ApprovalFlow.Contracts.Models;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

public class CategoryRulesEvaluatorTests
{
    [Fact]
    public void Meals_within_per_attendee_cap_is_compliant()
    {
        var invoice = Fixtures.Meals(total: 42m, attendees: 1);

        var result = CategoryRulesEvaluator.Evaluate(invoice, amountUsd: 42m);

        Assert.True(result.IsCompliant);
    }

    [Fact]
    public void Meals_missing_attendees_escalates_meal01()
    {
        var invoice = Fixtures.Meals(total: 42m, attendees: 0);

        var result = CategoryRulesEvaluator.Evaluate(invoice, amountUsd: 42m);

        Assert.Contains(result.EscalateViolations, v => v.RuleId == PolicyRuleIds.Meal01);
    }

    [Fact]
    public void Meals_client_dinner_over_500_without_client_notes_escalates_meal02()
    {
        var invoice = Fixtures.Meals(total: 1820m, attendees: 11, notes: "Weekend (Saturday). No client name provided.");

        var result = CategoryRulesEvaluator.Evaluate(invoice, amountUsd: 1820m);

        Assert.Contains(result.EscalateViolations, v => v.RuleId == PolicyRuleIds.Meal02);
    }

    [Fact]
    public void Meals_alcohol_only_rejects_meal03()
    {
        var invoice = Fixtures.Meals(total: 60m, attendees: 2,
            lineItems: new[] { new LineItem { Description = "Bottle of wine", Quantity = 1, UnitPrice = 60m } });

        var result = CategoryRulesEvaluator.Evaluate(invoice, amountUsd: 60m);

        Assert.True(result.HasReject);
        Assert.Contains(result.RejectViolations, v => v.RuleId == PolicyRuleIds.Meal03);
    }

    [Fact]
    public void Saas_at_or_under_200_is_compliant()
    {
        var invoice = Fixtures.Saas(200m);

        var result = CategoryRulesEvaluator.Evaluate(invoice, amountUsd: 200m);

        Assert.True(result.IsCompliant);
    }

    [Fact]
    public void Saas_over_200_escalates_saas01()
    {
        var invoice = Fixtures.Saas(220m);

        var result = CategoryRulesEvaluator.Evaluate(invoice, amountUsd: 220m);

        Assert.Contains(result.EscalateViolations, v => v.RuleId == PolicyRuleIds.Saas01);
    }

    [Fact]
    public void Hardware_over_1000_escalates_hw02_capital()
    {
        var invoice = Fixtures.Hardware(1400m);

        var result = CategoryRulesEvaluator.Evaluate(invoice, amountUsd: 1400m);

        Assert.Contains(result.EscalateViolations, v => v.RuleId == PolicyRuleIds.Hw02);
    }

    [Fact]
    public void Travel_over_1500_escalates_travel02()
    {
        var invoice = Fixtures.Travel(1600m);

        var result = CategoryRulesEvaluator.Evaluate(invoice, amountUsd: 1600m);

        Assert.Contains(result.EscalateViolations, v => v.RuleId == PolicyRuleIds.Travel02);
    }

    [Fact]
    public void Travel_business_class_escalates_travel03()
    {
        var invoice = Fixtures.Travel(800m, description: "Business class flight to Berlin");

        var result = CategoryRulesEvaluator.Evaluate(invoice, amountUsd: 800m);

        Assert.Contains(result.EscalateViolations, v => v.RuleId == PolicyRuleIds.Travel03);
    }
}
