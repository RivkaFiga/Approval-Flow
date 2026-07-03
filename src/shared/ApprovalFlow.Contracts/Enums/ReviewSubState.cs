namespace ApprovalFlow.Contracts.Enums;

/// <summary>
/// HITL sub-state carried by the <c>review.status</c> event (§5.2). These three values are a strict
/// subset of <see cref="LifecycleStatus"/>; Notification maps them 1-to-1:
/// <c>AwaitingApproval</c> → <see cref="LifecycleStatus.AwaitingApproval"/>,
/// <c>AwaitingInfo</c> → <see cref="LifecycleStatus.AwaitingInfo"/>,
/// <c>Paying</c> → <see cref="LifecycleStatus.Paying"/>.
/// A narrower type is used here so publishers cannot accidentally set terminal lifecycle values
/// (e.g. <c>Paid</c>) on a mid-flight <c>review.status</c> event.
/// </summary>
public enum ReviewSubState
{
    AwaitingApproval,
    AwaitingInfo,
    Paying
}
