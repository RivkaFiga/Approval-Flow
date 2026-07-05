using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Notification.Domain.Rules;

/// <summary>
/// Pure mapping from the mid-flight <see cref="ReviewSubState"/> on <c>review.status</c> to the projected
/// <see cref="LifecycleStatus"/> the submitter sees (§5.2, F2). The subset is deliberately narrower than
/// <see cref="LifecycleStatus"/> so a mid-flight event can never surface a terminal status like
/// <see cref="LifecycleStatus.Paid"/>.
/// </summary>
public static class ReviewSubStateMap
{
    public static LifecycleStatus ToLifecycle(ReviewSubState subState) => subState switch
    {
        ReviewSubState.AwaitingApproval => LifecycleStatus.AwaitingApproval,
        ReviewSubState.AwaitingInfo => LifecycleStatus.AwaitingInfo,
        ReviewSubState.Paying => LifecycleStatus.Paying,
        _ => throw new ArgumentOutOfRangeException(nameof(subState), subState, "Unknown review sub-state.")
    };
}
