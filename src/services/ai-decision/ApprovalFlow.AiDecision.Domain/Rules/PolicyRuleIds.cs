namespace ApprovalFlow.AiDecision.Domain.Rules;

/// <summary>
/// Stable policy rule identifiers (policy.md). Emitted in cited-rules lists for audit (§7).
/// </summary>
public static class PolicyRuleIds
{
    public const string GlobalReceipt = "GLOBAL-RECEIPT";
    public const string GlobalVendor = "GLOBAL-VENDOR";
    public const string GlobalFx = "GLOBAL-FX";
    public const string GlobalMath = "GLOBAL-MATH";
    public const string GlobalFraud = "GLOBAL-FRAUD";
    public const string GlobalDuplicate = "GLOBAL-DUP";

    public const string Meal01 = "MEAL-01";
    public const string Meal02 = "MEAL-02";
    public const string Meal03 = "MEAL-03";

    public const string Saas01 = "SAAS-01";

    public const string Hw01 = "HW-01";
    public const string Hw02 = "HW-02";

    public const string Travel01 = "TRAVEL-01";
    public const string Travel02 = "TRAVEL-02";
    public const string Travel03 = "TRAVEL-03";

    public const string AutonomyCeiling = "AUTONOMY-CEILING";
    public const string AutonomyConfidence = "AUTONOMY-CONFIDENCE";
}
