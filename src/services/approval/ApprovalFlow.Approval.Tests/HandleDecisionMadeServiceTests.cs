using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Application.Services;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Approval.Tests;

public class HandleDecisionMadeServiceTests
{
    private readonly IWorkflowInstanceRepository _repo = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IApprovalWorkflowScheduler _scheduler = Substitute.For<IApprovalWorkflowScheduler>();
    private readonly HandleDecisionMadeService _sut;

    public HandleDecisionMadeServiceTests()
    {
        _sut = new HandleDecisionMadeService(_repo, _scheduler, NullLogger<HandleDecisionMadeService>.Instance);
    }

    private static DecisionMadeV1 Event(Route route, string trackingId = "TRK-1") => new()
    {
        TrackingId = trackingId,
        CorrelationId = "corr-1",
        OccurredAt = DateTimeOffset.UtcNow,
        Route = route,
        Recommendation = Recommendation.Approve,
        Confidence = 0.9,
        AmountUsd = 199.99m,
        Department = "engineering-2026Q2",
        CitedRules = new[] { new PolicyViolation { RuleId = "SAAS-01", Detail = "Above monthly cap." } }
    };

    [Fact]
    public async Task Existing_trackingId_is_no_op()
    {
        _repo.ExistsByTrackingIdAsync("TRK-1", Arg.Any<CancellationToken>()).Returns(true);

        await _sut.HandleAsync(Event(Route.HumanReview), CancellationToken.None);

        await _scheduler.DidNotReceive().ScheduleAsync(Arg.Any<DecisionMadeV1>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(Route.HumanReview)]
    [InlineData(Route.AutoApprove)]
    [InlineData(Route.Reject)]
    [InlineData(Route.Duplicate)]
    public async Task New_trackingId_schedules_workflow(Route route)
    {
        _repo.ExistsByTrackingIdAsync("TRK-1", Arg.Any<CancellationToken>()).Returns(false);

        await _sut.HandleAsync(Event(route), CancellationToken.None);

        await _scheduler.Received(1).ScheduleAsync(
            Arg.Is<DecisionMadeV1>(e => e.TrackingId == "TRK-1" && e.Route == route),
            Arg.Any<CancellationToken>());
    }
}
