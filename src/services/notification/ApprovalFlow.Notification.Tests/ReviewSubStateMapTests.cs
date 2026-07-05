using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Notification.Domain.Rules;
using Xunit;

namespace ApprovalFlow.Notification.Tests;

public class ReviewSubStateMapTests
{
    [Theory]
    [InlineData(ReviewSubState.AwaitingApproval, LifecycleStatus.AwaitingApproval)]
    [InlineData(ReviewSubState.AwaitingInfo, LifecycleStatus.AwaitingInfo)]
    [InlineData(ReviewSubState.Paying, LifecycleStatus.Paying)]
    public void Maps_each_subState_to_its_lifecycle(ReviewSubState subState, LifecycleStatus expected)
    {
        Assert.Equal(expected, ReviewSubStateMap.ToLifecycle(subState));
    }
}
