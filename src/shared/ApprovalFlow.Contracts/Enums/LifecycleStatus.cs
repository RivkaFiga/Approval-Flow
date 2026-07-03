namespace ApprovalFlow.Contracts.Enums;

/// <summary>
/// Live submission status projected by Notification from the full event lifecycle (§4, §5.2):
/// <c>invoice.submitted</c> → <c>received</c>; <c>decision.made</c> → <c>under-review</c>;
/// <c>review.status</c> → <c>awaiting-approval</c> | <c>awaiting-info</c> | <c>paying</c>;
/// <c>item.finalized</c> → <c>paid</c> | <c>rejected</c> | <c>payment-failed</c> | <c>duplicate</c>.
/// </summary>
public enum LifecycleStatus
{
    Received,
    UnderReview,
    AwaitingApproval,
    AwaitingInfo,
    Paying,
    Paid,
    Rejected,
    PaymentFailed,
    Duplicate
}
