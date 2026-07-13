using ApprovalFlow.AiDecision.Domain.Rules;
using ApprovalFlow.Contracts.Models;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

public class PreCheckEvaluatorTests
{
    [Fact]
    public void Usd_invoice_amount_is_taken_as_is()
    {
        var invoice = Fixtures.Saas(100m);

        var result = PreCheckEvaluator.Evaluate(invoice, Fixtures.DefaultPolicy());

        Assert.Equal(100m, result.AmountUsd);
        Assert.True(result.MathReconciles);
        Assert.False(result.HasHardStop);
    }

    [Fact]
    public void Fx_over_1000_usd_triggers_global_fx_hard_stop()
    {
        var invoice = Fixtures.Meals(1200m, attendees: 4, currency: "EUR",
            lineItems: new[] { new LineItem { Description = "Meal", Quantity = 1, UnitPrice = 1200m } });

        var result = PreCheckEvaluator.Evaluate(invoice, Fixtures.DefaultPolicy());

        Assert.Equal(1296m, result.AmountUsd);
        Assert.Contains(result.HardStops, v => v.RuleId == PolicyRuleIds.GlobalFx);
    }

    [Fact]
    public void Math_mismatch_triggers_global_math()
    {
        var invoice = Fixtures.Saas(3000m) with { TaxAmount = 0m };
        invoice = invoice with { LineItems = new[] { new LineItem { Description = "Sub", Quantity = 1, UnitPrice = 300m } } };

        var result = PreCheckEvaluator.Evaluate(invoice, Fixtures.DefaultPolicy());

        Assert.False(result.MathReconciles);
        Assert.Contains(result.HardStops, v => v.RuleId == PolicyRuleIds.GlobalMath);
    }

    [Fact]
    public void Missing_receipt_over_25_triggers_global_receipt()
    {
        var invoice = Fixtures.Meals(120m, attendees: 4, receipt: false);

        var result = PreCheckEvaluator.Evaluate(invoice, Fixtures.DefaultPolicy());

        Assert.Contains(result.HardStops, v => v.RuleId == PolicyRuleIds.GlobalReceipt);
    }

    [Fact]
    public void Unknown_vendor_triggers_global_vendor()
    {
        var invoice = Fixtures.Meals(50m, attendees: 1, vendor: "Fly-by-Night Cafe", vendorKnown: false);

        var result = PreCheckEvaluator.Evaluate(invoice, Fixtures.DefaultPolicy());

        Assert.Contains(result.HardStops, v => v.RuleId == PolicyRuleIds.GlobalVendor);
    }

    [Fact]
    public void Receipt_not_required_under_25()
    {
        var invoice = Fixtures.Meals(20m, attendees: 1, receipt: false,
            lineItems: new[] { new LineItem { Description = "Meal", Quantity = 1, UnitPrice = 20m } });

        var result = PreCheckEvaluator.Evaluate(invoice, Fixtures.DefaultPolicy());

        Assert.DoesNotContain(result.HardStops, v => v.RuleId == PolicyRuleIds.GlobalReceipt);
    }
}
