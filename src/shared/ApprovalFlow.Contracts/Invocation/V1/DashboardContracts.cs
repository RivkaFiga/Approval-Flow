namespace ApprovalFlow.Contracts.Invocation.V1;

/// <summary>
/// Aggregated controller dashboard summary produced from the notification projection store (F8).
/// All counts and amounts reflect the state of the <c>SubmissionStatus</c> read model at the time of the query.
/// </summary>
public sealed record DashboardSummaryResponse
{
    /// <summary>Number of submissions for which a routing decision has been recorded.</summary>
    public int TotalProcessed { get; init; }

    public int AutoApprovedCount { get; init; }
    public int HumanApprovalCount { get; init; }

    /// <summary>Fraction of processed submissions routed to human review (0.0–1.0).</summary>
    public double EscalationRate { get; init; }

    public decimal AutoApprovedAmountUsd { get; init; }
    public decimal HumanApprovedAmountUsd { get; init; }
}
