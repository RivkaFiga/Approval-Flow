using ApprovalFlow.AiDecision.Application.Ports;
using ApprovalFlow.AiDecision.Application.Services;
using ApprovalFlow.AiDecision.Domain.Entities;
using ApprovalFlow.AiDecision.Domain.Values;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Invocation.V1;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

public class DecideInvoiceServiceTests
{
    private readonly IPolicySnapshotProvider _policy = Substitute.For<IPolicySnapshotProvider>();
    private readonly IPolicyAgent _agent = Substitute.For<IPolicyAgent>();
    private readonly IDecisionRepository _repo = Substitute.For<IDecisionRepository>();
    private readonly IDecisionEventPublisher _publisher = Substitute.For<IDecisionEventPublisher>();
    private readonly DecideInvoiceService _sut;

    public DecideInvoiceServiceTests()
    {
        _policy.GetAsync(Arg.Any<CancellationToken>()).Returns(Fixtures.DefaultPolicy());
        _agent.RecommendAsync(Arg.Any<Contracts.Models.Invoice>(), Arg.Any<PolicySnapshotResponse>(), Arg.Any<CancellationToken>())
            .Returns(new AgentRecommendation { Recommendation = Recommendation.Approve, Confidence = 0.9 });

        _sut = new DecideInvoiceService(_policy, _agent, _repo, _publisher, NullLogger<DecideInvoiceService>.Instance);
    }

    private static InvoiceSubmittedV1 Event(string trackingId = "TRK-1") => new()
    {
        TrackingId = trackingId,
        CorrelationId = "corr-1",
        OccurredAt = DateTimeOffset.UtcNow,
        Invoice = Fixtures.Saas(99m)
    };

    [Fact]
    public async Task Publishes_decision_made_and_persists_audit_record_for_new_event()
    {
        _repo.ExistsByTrackingIdAsync("TRK-1").Returns(false);
        DecisionMadeV1? captured = null;
        await _publisher.PublishDecisionMadeAsync(Arg.Do<DecisionMadeV1>(e => captured = e), Arg.Any<CancellationToken>());

        await _sut.DecideAsync(Event(), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<Decision>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.NotNull(captured);
        Assert.Equal(Route.AutoApprove, captured!.Route);
        Assert.Equal("TRK-1", captured.TrackingId);
        Assert.Equal("corr-1", captured.CorrelationId);
        Assert.Equal(99m, captured.AmountUsd);
    }

    [Fact]
    public async Task Redelivered_event_is_no_op()
    {
        _repo.ExistsByTrackingIdAsync("TRK-1").Returns(true);

        await _sut.DecideAsync(Event(), CancellationToken.None);

        await _repo.DidNotReceive().AddAsync(Arg.Any<Decision>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishDecisionMadeAsync(Arg.Any<DecisionMadeV1>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Escalates_to_human_review_when_amount_exceeds_ceiling()
    {
        _repo.ExistsByTrackingIdAsync(Arg.Any<string>()).Returns(false);
        DecisionMadeV1? captured = null;
        await _publisher.PublishDecisionMadeAsync(Arg.Do<DecisionMadeV1>(e => captured = e), Arg.Any<CancellationToken>());

        var @event = Event() with { Invoice = Fixtures.Saas(300m) };

        await _sut.DecideAsync(@event, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(Route.HumanReview, captured!.Route);
    }
}
