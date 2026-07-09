using ApprovalFlow.AiDecision.Api.Subscribers;
using ApprovalFlow.AiDecision.Infrastructure.Policy;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

public class PolicyChangedSubscriberTests
{
    private readonly DaprClient _dapr = Substitute.For<DaprClient>();
    private readonly DaprConfigPolicySnapshotProvider _provider;
    private readonly PolicyChangedSubscriber _sut;

    public PolicyChangedSubscriberTests()
    {
        var fallback = new ConfigPolicySnapshotProvider(new StaticOptionsMonitor(new PolicySnapshotOptions()));
        _dapr.CreateInvokeMethodRequest(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => new HttpRequestMessage(HttpMethod.Get, "http://config-policy/api/policy-snapshot"));

        _provider = new DaprConfigPolicySnapshotProvider(_dapr, fallback, NullLogger<DaprConfigPolicySnapshotProvider>.Instance);
        _sut = new PolicyChangedSubscriber(_provider, NullLogger<PolicyChangedSubscriber>.Instance);
    }

    [Fact]
    public async Task Handle_invalidates_the_cached_snapshot()
    {
        _dapr.InvokeMethodAsync<PolicySnapshotResponse>(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromResult(new PolicySnapshotResponse { Version = "policy-v1", Thresholds = new AutonomyThresholds { CeilingUsd = 100m } }),
                _ => Task.FromResult(new PolicySnapshotResponse { Version = "policy-v2", Thresholds = new AutonomyThresholds { CeilingUsd = 500m } }));

        var before = await _provider.GetAsync();

        var result = _sut.Handle(new PolicyChangedSubscriber.PolicyChangedEvent(Guid.NewGuid(), 2, DateTimeOffset.UtcNow));

        var after = await _provider.GetAsync();

        Assert.IsType<OkResult>(result);
        Assert.Equal("policy-v1", before.Version);
        Assert.Equal("policy-v2", after.Version);
    }

    [Fact]
    public void Handle_is_safe_when_cache_is_already_empty()
    {
        var result = _sut.Handle(new PolicyChangedSubscriber.PolicyChangedEvent(Guid.NewGuid(), 1, DateTimeOffset.UtcNow));
        Assert.IsType<OkResult>(result);
    }

    private sealed class StaticOptionsMonitor : IOptionsMonitor<PolicySnapshotOptions>
    {
        public StaticOptionsMonitor(PolicySnapshotOptions value) => CurrentValue = value;
        public PolicySnapshotOptions CurrentValue { get; }
        public PolicySnapshotOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<PolicySnapshotOptions, string?> listener) => null;
    }
}
