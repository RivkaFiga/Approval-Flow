namespace ApprovalFlow.Payment.Infrastructure.Configuration;

/// <summary>
/// Injectable-failure controls for <c>SimulatedPaymentProvider</c> (§1.4, INV-1012). Reads flat lists of
/// <c>paymentId</c> / <c>trackingId</c> values that should force the provider to return
/// <see cref="Domain.Values.PaymentProviderOutcome.Failed"/>. Any match on either list triggers failure.
/// </summary>
public sealed class SimulatedPaymentProviderOptions
{
    /// <summary>Payment ids that should fail on charge.</summary>
    public List<string> FailPaymentIds { get; set; } = new();

    /// <summary>Tracking ids whose charge should fail (useful for fixture-based scenarios like INV-1012).</summary>
    public List<string> FailTrackingIds { get; set; } = new();

    /// <summary>Reason surfaced by <see cref="Domain.Values.ChargeResult.Failure"/> for forced failures.</summary>
    public string FailureReason { get; set; } = "Simulated provider failure (INV-1012).";
}
