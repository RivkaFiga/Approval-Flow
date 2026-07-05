using ApprovalFlow.Contracts.Events.V1;

namespace ApprovalFlow.Approval.Application.Ports;

/// <summary>
/// Publishing port for the two events the workflow can emit after ingesting a <c>decision.made</c> (§5.2):
/// <see cref="ReviewStatusV1"/> (<c>awaiting-approval</c> — the "ApprovalRequired" branch) and
/// <see cref="ItemFinalizedV1"/> (the "WorkflowCompleted" branch).
/// </summary>
public interface IWorkflowEventPublisher
{
    Task PublishReviewStatusAsync(ReviewStatusV1 @event, CancellationToken ct = default);
    Task PublishItemFinalizedAsync(ItemFinalizedV1 @event, CancellationToken ct = default);
}
