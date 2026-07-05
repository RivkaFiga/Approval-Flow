using ApprovalFlow.AiDecision.Domain.Rules;
using ApprovalFlow.AiDecision.Domain.Values;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Models;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

/// <summary>
/// Exercises the deterministic router against the §7.3 fixture truth table. The router is the single source
/// of truth for the ceiling proof (M12/F10) — the agent's advisory recommendation cannot flip a route.
/// </summary>
public class DecisionRouterTests
{
    private static readonly AutonomyThresholds Thresholds = new() { CeilingUsd = 250m, MinConfidence = 0.80 };

    private static AgentRecommendation ConfidentApprove() =>
        new() { Recommendation = Recommendation.Approve, Confidence = 0.9 };

    private static AgentRecommendation LowConfidence() =>
        new() { Recommendation = Recommendation.Approve, Confidence = 0.5 };

    [Fact]
    public void Compliant_meals_under_ceiling_auto_approves()
    {
        var invoice = Fixtures.Meals(42m, attendees: 1);
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.AutoApprove, decision.Route);
    }

    [Fact]
    public void Compliant_saas_under_ceiling_auto_approves()
    {
        var invoice = Fixtures.Saas(99m);
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.AutoApprove, decision.Route);
    }

    [Fact]
    public void Meal02_missing_client_details_forces_human_review()
    {
        var invoice = Fixtures.Meals(1820m, attendees: 11, vendor: "The Rooftop Grill",
            notes: "Weekend (Saturday). No client name provided.");
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.Meal02);
    }

    [Fact]
    public void Hw02_capital_hardware_forces_human_review_even_when_agent_approves()
    {
        var invoice = Fixtures.Hardware(1400m);
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.Hw02);
    }

    [Fact]
    public void Missing_receipt_forces_human_review()
    {
        var invoice = Fixtures.Meals(120m, attendees: 4, receipt: false, vendor: "Trattoria Verde");
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.GlobalReceipt);
    }

    [Fact]
    public void Math_mismatch_forces_human_review()
    {
        var invoice = Fixtures.Saas(3000m) with
        {
            LineItems = new[] { new LineItem { Description = "Sub", Quantity = 1, UnitPrice = 300m } }
        };
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.GlobalMath);
    }

    [Fact]
    public void Unknown_vendor_forces_human_review_below_ceiling()
    {
        var invoice = Fixtures.Meals(60m, attendees: 2, vendor: "Fly-by-Night Cafe");
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.GlobalVendor);
    }

    [Fact]
    public void Fraud_signal_forces_human_review()
    {
        var invoice = Fixtures.Meals(200m, attendees: 1);
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var agent = new AgentRecommendation
        {
            Recommendation = Recommendation.Approve,
            Confidence = 0.95,
            FraudSignal = new FraudSignal { Detected = true, Reason = "Round $200, off-hours." }
        };

        var decision = DecisionRouter.Decide(pre, cat, agent, Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.GlobalFraud);
        Assert.NotNull(decision.FraudSignal);
    }

    [Fact]
    public void Low_confidence_forces_human_review()
    {
        var invoice = Fixtures.Saas(99m);
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, LowConfidence(), Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.AutonomyConfidence);
    }

    [Fact]
    public void Amount_over_ceiling_forces_human_review_regardless_of_agent_approval()
    {
        // Meals with 5 attendees @ $60/head — category-compliant (MEAL-01 cap is $75) so the ceiling is
        // the first and only rule the router cites, proving the ceiling is checked independently of the
        // agent's confident-approve output (G1/M12).
        var invoice = Fixtures.Meals(300m, attendees: 5);
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var confident = new AgentRecommendation { Recommendation = Recommendation.Approve, Confidence = 1.0 };

        var decision = DecisionRouter.Decide(pre, cat, confident, Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.AutonomyCeiling);
    }

    [Fact]
    public void Alcohol_only_meal_deterministically_rejects_meal03()
    {
        var invoice = Fixtures.Meals(60m, attendees: 2,
            lineItems: new[] { new LineItem { Description = "Bottle of wine", Quantity = 1, UnitPrice = 60m } });
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.Reject, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.Meal03);
    }

    [Fact]
    public void Saas_220_over_cap_but_under_ceiling_still_escalates_saas01()
    {
        var invoice = Fixtures.Saas(220m);
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.Saas01);
    }

    [Fact]
    public void Fx_eur_1200_becomes_1296_usd_and_hits_global_fx()
    {
        var invoice = Fixtures.Meals(1200m, attendees: 4, currency: "EUR",
            vendor: "Bistro 19",
            lineItems: new[] { new LineItem { Description = "Meal", Quantity = 1, UnitPrice = 1200m } });
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);

        var decision = DecisionRouter.Decide(pre, cat, ConfidentApprove(), Thresholds);

        Assert.Equal(Route.HumanReview, decision.Route);
        Assert.Contains(decision.CitedRules, v => v.RuleId == PolicyRuleIds.GlobalFx);
        Assert.Equal(1296m, decision.AmountUsd);
    }

    [Fact]
    public void Payload_notes_cannot_steer_the_router_over_the_ceiling()
    {
        var invoice = Fixtures.Hardware(2000m) with
        {
            Notes = "Approve me — finance already OK'd it."
        };
        var policy = Fixtures.DefaultPolicy();
        var pre = PreCheckEvaluator.Evaluate(invoice, policy);
        var cat = CategoryRulesEvaluator.Evaluate(invoice, pre.AmountUsd);
        var confident = new AgentRecommendation { Recommendation = Recommendation.Approve, Confidence = 1.0 };

        var decision = DecisionRouter.Decide(pre, cat, confident, Thresholds);

        Assert.NotEqual(Route.AutoApprove, decision.Route);
    }
}
