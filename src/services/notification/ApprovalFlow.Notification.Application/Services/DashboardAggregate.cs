namespace ApprovalFlow.Notification.Application.Services;

/// <summary>Raw aggregate counts and amounts returned by the dashboard repository before mapping to the response DTO.</summary>
public sealed record DashboardAggregate(
    int TotalProcessed,
    int AutoApprovedCount,
    int HumanApprovalCount,
    decimal AutoApprovedAmountUsd,
    decimal HumanApprovedAmountUsd
);
