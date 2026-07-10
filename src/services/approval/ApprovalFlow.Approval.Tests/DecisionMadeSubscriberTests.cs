using ApprovalFlow.Approval.Api.Subscribers;
using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Approval.Tests;

public class DecisionMadeSubscriberTests
{
    private readonly IApprovalWorkflowScheduler _scheduler = Substitute.For<IApprovalWorkflowScheduler>();
    private readonly DecisionMadeSubscriber _sut;

    public DecisionMadeSubscriberTests()
    {
        _sut = new DecisionMadeSubscriber(_scheduler, NullLogger<DecisionMadeSubscriber>.Instance);
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

    [Theory]
    [InlineData(Route.HumanReview)]
    [InlineData(Route.AutoApprove)]
    [InlineData(Route.Reject)]
    [InlineData(Route.Duplicate)]
    public async Task Handle_schedules_workflow_for_any_route(Route route)
    {
        var @event = Event(route);

        var result = await _sut.Handle(@event, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        await _scheduler.Received(1).ScheduleAsync(
            Arg.Is<DecisionMadeV1>(e => e.TrackingId == "TRK-1" && e.Route == route),
            Arg.Any<CancellationToken>());
    }
}
