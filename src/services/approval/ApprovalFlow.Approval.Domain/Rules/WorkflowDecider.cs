using ApprovalFlow.Approval.Domain.Values;
using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Approval.Domain.Rules;

/// <summary>
/// Pure domain rule (no I/O, no framework types) that converts the router's <see cref="Route"/> into the
/// workflow's next step and its persisted <see cref="WorkflowState"/>. Owns the "auto-approve OR require
/// human approval" branching so it is unit-testable in isolation from Dapr and the DB.
/// </summary>
public static class WorkflowDecider
{
    /// <summary>What the workflow must do next after ingesting a <c>decision.made</c> event (§9).</summary>
    public static WorkflowNextStep NextStepFor(Route route) => route switch
    {
        Route.HumanReview => WorkflowNextStep.RequireHumanApproval,
        Route.AutoApprove => WorkflowNextStep.Complete,
        Route.Reject => WorkflowNextStep.Complete,
        Route.Duplicate => WorkflowNextStep.Complete,
        _ => throw new ArgumentOutOfRangeException(nameof(route), route, "Unknown route.")
    };

    /// <summary>The persisted workflow state for a given router <see cref="Route"/>.</summary>
    public static WorkflowState StateFor(Route route) => route switch
    {
        Route.HumanReview => WorkflowState.AwaitingApproval,
        Route.AutoApprove => WorkflowState.AutoApproved,
        Route.Reject => WorkflowState.Rejected,
        Route.Duplicate => WorkflowState.Duplicated,
        _ => throw new ArgumentOutOfRangeException(nameof(route), route, "Unknown route.")
    };
}
