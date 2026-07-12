using ApprovalFlow.AiDecision.Api.Subscribers;
using ApprovalFlow.AiDecision.Application.Ports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

public class PolicyChangedSubscriberTests
{
    private readonly IPolicySnapshotRefresher _refresher = Substitute.For<IPolicySnapshotRefresher>();
    private readonly PolicyChangedSubscriber _sut;

    public PolicyChangedSubscriberTests()
    {
        _sut = new PolicyChangedSubscriber(_refresher, NullLogger<PolicyChangedSubscriber>.Instance);
    }

    [Fact]
    public void Handle_calls_invalidate_on_the_refresher_port()
    {
        var result = _sut.Handle(new PolicyChangedSubscriber.PolicyChangedEvent(Guid.NewGuid(), 2, DateTimeOffset.UtcNow));

        Assert.IsType<OkResult>(result);
        _refresher.Received(1).Invalidate();
    }

    [Fact]
    public void Handle_is_safe_when_called_repeatedly()
    {
        _sut.Handle(new PolicyChangedSubscriber.PolicyChangedEvent(Guid.NewGuid(), 1, DateTimeOffset.UtcNow));
        _sut.Handle(new PolicyChangedSubscriber.PolicyChangedEvent(Guid.NewGuid(), 2, DateTimeOffset.UtcNow));

        _refresher.Received(2).Invalidate();
    }
}
