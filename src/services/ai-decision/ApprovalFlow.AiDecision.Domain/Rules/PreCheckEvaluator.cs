using ApprovalFlow.AiDecision.Domain.Values;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;

namespace ApprovalFlow.AiDecision.Domain.Rules;

/// <summary>
/// Deterministic pre-checks (§7 C1–C5): FX conversion to USD, math reconciliation, receipt-if-over-$25, and
/// vendor-known verification against the known-vendor list. Pure — no I/O.
/// </summary>
public static class PreCheckEvaluator
{
    private const decimal MathTolerance = 0.01m;
    private const decimal ReceiptRequiredAboveUsd = 25m;
    private const decimal FxHardStopUsd = 1000m;

    public static PreCheckResult Evaluate(Invoice invoice, PolicySnapshotResponse policy)
    {
        var amountUsd = ConvertToUsd(invoice.Total, invoice.Currency, policy);
        var mathReconciles = MathReconciles(invoice);

        var hardStops = new List<PolicyViolation>();

        if (!mathReconciles)
            hardStops.Add(new PolicyViolation { RuleId = PolicyRuleIds.GlobalMath, Detail = "Line items + tax do not reconcile to total." });

        if (amountUsd > ReceiptRequiredAboveUsd && !invoice.ReceiptPresent)
            hardStops.Add(new PolicyViolation { RuleId = PolicyRuleIds.GlobalReceipt, Detail = "Receipt required for expenses over $25." });

        if (!IsVendorKnown(invoice, policy))
            hardStops.Add(new PolicyViolation { RuleId = PolicyRuleIds.GlobalVendor, Detail = $"Unknown vendor '{invoice.Vendor}'." });

        var isForeignCurrency = !string.Equals(invoice.Currency, policy.BaseCurrency, StringComparison.OrdinalIgnoreCase);
        if (isForeignCurrency && amountUsd > FxHardStopUsd)
            hardStops.Add(new PolicyViolation { RuleId = PolicyRuleIds.GlobalFx, Detail = $"Foreign-currency amount ${amountUsd:F2} exceeds $1,000 hard stop." });

        return new PreCheckResult
        {
            AmountUsd = amountUsd,
            MathReconciles = mathReconciles,
            HardStops = hardStops
        };
    }

    private static decimal ConvertToUsd(decimal amount, string currency, PolicySnapshotResponse policy)
    {
        if (string.Equals(currency, policy.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        if (policy.FxRates.TryGetValue(currency.ToUpperInvariant(), out var rate) && rate > 0m)
            return decimal.Round(amount * rate, 2, MidpointRounding.AwayFromZero);

        return amount;
    }

    private static bool MathReconciles(Invoice invoice)
    {
        var lineTotal = invoice.LineItems.Sum(li => li.Quantity * li.UnitPrice);
        var expected = lineTotal + invoice.TaxAmount;
        return Math.Abs(expected - invoice.Total) <= MathTolerance;
    }

    private static bool IsVendorKnown(Invoice invoice, PolicySnapshotResponse policy)
    {
        if (policy.KnownVendors.Any(v => string.Equals(v, invoice.Vendor, StringComparison.OrdinalIgnoreCase)))
            return true;

        return invoice.VendorKnown && policy.KnownVendors.Count == 0;
    }
}
