using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Notification.Domain.Entities;
using Xunit;

namespace ApprovalFlow.Notification.Tests;

public class SubmissionStatusTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    private static SubmissionStatus Received() =>
        SubmissionStatus.CreateReceived("TRK-1", "corr-1", T0);

    [Fact]
    public void CreateReceived_starts_at_received_with_no_route_or_reason()
    {
        var status = Received();

        Assert.Equal(LifecycleStatus.Received, status.CurrentStatus);
        Assert.Null(status.CurrentRoute);
        Assert.Null(status.Reason);
        Assert.Null(status.AmountUsd);
        Assert.Equal(T0, status.UpdatedAt);
    }

    [Fact]
    public void ApplyDecision_advances_to_under_review_and_records_route_and_amount()
    {
        var status = Received();

        status.ApplyDecision(Route.HumanReview, 199.99m, T0.AddSeconds(1));

        Assert.Equal(LifecycleStatus.UnderReview, status.CurrentStatus);
        Assert.Equal(Route.HumanReview, status.CurrentRoute);
        Assert.Equal(199.99m, status.AmountUsd);
    }

    [Fact]
    public void ApplyReviewSubState_awaiting_info_records_what_we_still_need_as_reason()
    {
        var status = Received();
        status.ApplyDecision(Route.HumanReview, 100m, T0.AddSeconds(1));

        status.ApplyReviewSubState(ReviewSubState.AwaitingInfo, "Need attendee count.", T0.AddSeconds(2));

        Assert.Equal(LifecycleStatus.AwaitingInfo, status.CurrentStatus);
        Assert.Equal("Need attendee count.", status.Reason);
    }

    [Fact]
    public void ApplyReviewSubState_awaiting_approval_does_not_touch_reason()
    {
        var status = Received();

        status.ApplyReviewSubState(ReviewSubState.AwaitingApproval, whatWeStillNeed: null, T0.AddSeconds(1));

        Assert.Equal(LifecycleStatus.AwaitingApproval, status.CurrentStatus);
        Assert.Null(status.Reason);
    }

    [Fact]
    public void ApplyReviewSubState_clears_stale_awaiting_info_reason_when_transitioning_onward()
    {
        var status = Received();
        status.ApplyDecision(Route.HumanReview, 100m, T0.AddSeconds(1));
        status.ApplyReviewSubState(ReviewSubState.AwaitingInfo, "Need attendee count.", T0.AddSeconds(2));

        // Submitter provided info → workflow resumes at AwaitingApproval. The prior send-back reason
        // must not linger — otherwise F2 shows "Need attendee count." while the item is being reviewed.
        status.ApplyReviewSubState(ReviewSubState.AwaitingApproval, null, T0.AddSeconds(3));

        Assert.Equal(LifecycleStatus.AwaitingApproval, status.CurrentStatus);
        Assert.Null(status.Reason);
    }

    [Fact]
    public void ApplyFinalized_records_terminal_status_reason_and_payment_outcome()
    {
        var status = Received();

        status.ApplyFinalized(LifecycleStatus.Paid, "Auto-approved.", PaymentOutcome.Paid, T0.AddSeconds(1));

        Assert.Equal(LifecycleStatus.Paid, status.CurrentStatus);
        Assert.Equal("Auto-approved.", status.Reason);
        Assert.Equal(PaymentOutcome.Paid, status.CurrentPaymentOutcome);
    }

    [Fact]
    public void Older_or_equal_OccurredAt_is_a_no_op_replay()
    {
        var status = Received();
        status.ApplyDecision(Route.AutoApprove, 50m, T0.AddSeconds(5));

        // Replay of an earlier stage should NOT roll the state back.
        status.ApplyReviewSubState(ReviewSubState.AwaitingApproval, null, T0.AddSeconds(1));
        Assert.Equal(LifecycleStatus.UnderReview, status.CurrentStatus);

        // Same timestamp as the last write is treated as a replay too.
        status.ApplyReviewSubState(ReviewSubState.AwaitingApproval, null, T0.AddSeconds(5));
        Assert.Equal(LifecycleStatus.UnderReview, status.CurrentStatus);
    }

    [Fact]
    public void Finalized_terminal_status_survives_a_late_review_status_replay()
    {
        var status = Received();
        status.ApplyDecision(Route.HumanReview, 100m, T0.AddSeconds(1));
        status.ApplyFinalized(LifecycleStatus.Paid, "Paid.", PaymentOutcome.Paid, T0.AddSeconds(10));

        status.ApplyReviewSubState(ReviewSubState.AwaitingApproval, null, T0.AddSeconds(2));

        Assert.Equal(LifecycleStatus.Paid, status.CurrentStatus);
    }
}
