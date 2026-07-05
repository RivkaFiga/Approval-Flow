using ApprovalFlow.Approval.Domain.Rules;
using ApprovalFlow.Approval.Domain.Values;
using ApprovalFlow.Contracts.Enums;
using Xunit;

namespace ApprovalFlow.Approval.Tests;

public class WorkflowDeciderTests
{
    [Theory]
    [InlineData(Route.HumanReview, WorkflowNextStep.RequireHumanApproval)]
    [InlineData(Route.AutoApprove, WorkflowNextStep.Complete)]
    [InlineData(Route.Reject, WorkflowNextStep.Complete)]
    [InlineData(Route.Duplicate, WorkflowNextStep.Complete)]
    public void NextStepFor_returns_expected_branch(Route route, WorkflowNextStep expected)
    {
        Assert.Equal(expected, WorkflowDecider.NextStepFor(route));
    }

    [Theory]
    [InlineData(Route.HumanReview, WorkflowState.AwaitingApproval)]
    [InlineData(Route.AutoApprove, WorkflowState.AutoApproved)]
    [InlineData(Route.Reject, WorkflowState.Rejected)]
    [InlineData(Route.Duplicate, WorkflowState.Duplicated)]
    public void StateFor_returns_expected_state(Route route, WorkflowState expected)
    {
        Assert.Equal(expected, WorkflowDecider.StateFor(route));
    }
}
