using ApprovalFlow.AiDecision.Infrastructure.Policy;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;
using Dapr.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

/// <summary>
/// DaprClient exposes a non-virtual convenience overload
/// <c>InvokeMethodAsync&lt;TResponse&gt;(HttpMethod, appId, methodName, ct)</c> that internally calls two
/// abstract primitives: <c>CreateInvokeMethodRequest</c> (to build the request) and
/// <c>InvokeMethodAsync&lt;TResponse&gt;(HttpRequestMessage, ct)</c> (to execute it). We substitute those
/// two abstract methods.
/// </summary>
public class DaprConfigPolicySnapshotProviderTests
{
    private readonly DaprClient _dapr = Substitute.For<DaprClient>();
    private readonly ConfigPolicySnapshotProvider _fallback;
    private readonly DaprConfigPolicySnapshotProvider _sut;

    public DaprConfigPolicySnapshotProviderTests()
    {
        var fallbackOptions = new PolicySnapshotOptions
        {
            Version = "fallback-v0",
            BaseCurrency = "USD",
            CeilingUsd = 100m,
            MinConfidence = 0.5,
            FxRates = new Dictionary<string, decimal> { ["EUR"] = 1.0m },
            KnownVendors = new List<string> { "FallbackVendor" }
        };
        _fallback = new ConfigPolicySnapshotProvider(new StaticOptionsMonitor(fallbackOptions));

        _dapr.CreateInvokeMethodRequest(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => new HttpRequestMessage(HttpMethod.Get, "http://config-policy/api/policy-snapshot"));

        _sut = new DaprConfigPolicySnapshotProvider(_dapr, _fallback, NullLogger<DaprConfigPolicySnapshotProvider>.Instance);
    }

    private static PolicySnapshotResponse RemoteSnapshot(string version) => new()
    {
        Version = version,
        BaseCurrency = "USD",
        Thresholds = new AutonomyThresholds { CeilingUsd = 250m, MinConfidence = 0.80 },
        FxRates = new Dictionary<string, decimal> { ["EUR"] = 1.08m },
        KnownVendors = new[] { "Bistro 19" }
    };

    private void MockRemote(params PolicySnapshotResponse[] responses)
    {
        _dapr.InvokeMethodAsync<PolicySnapshotResponse>(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(responses[0], responses.Skip(1).ToArray());
    }

    [Fact]
    public async Task GetAsync_cache_miss_fetches_from_config_policy()
    {
        MockRemote(RemoteSnapshot("policy-v2"));

        var actual = await _sut.GetAsync();

        Assert.Equal("policy-v2", actual.Version);
        Assert.Equal(250m, actual.Thresholds.CeilingUsd);
        await _dapr.Received(1).InvokeMethodAsync<PolicySnapshotResponse>(
            Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        _dapr.Received(1).CreateInvokeMethodRequest(HttpMethod.Get, "config-policy", "api/policy-snapshot");
    }

    [Fact]
    public async Task GetAsync_cache_hit_does_not_call_dapr_a_second_time()
    {
        MockRemote(RemoteSnapshot("policy-v2"));

        var first = await _sut.GetAsync();
        var second = await _sut.GetAsync();
        var third = await _sut.GetAsync();

        Assert.Same(first, second);
        Assert.Same(second, third);
        await _dapr.Received(1).InvokeMethodAsync<PolicySnapshotResponse>(
            Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalidate_forces_a_fresh_fetch_on_next_get()
    {
        MockRemote(RemoteSnapshot("policy-v2"), RemoteSnapshot("policy-v3"));

        var before = await _sut.GetAsync();
        _sut.Invalidate();
        var after = await _sut.GetAsync();

        Assert.Equal("policy-v2", before.Version);
        Assert.Equal("policy-v3", after.Version);
        await _dapr.Received(2).InvokeMethodAsync<PolicySnapshotResponse>(
            Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalidate_is_idempotent_across_repeated_calls()
    {
        MockRemote(RemoteSnapshot("policy-v2"), RemoteSnapshot("policy-v3"));

        await _sut.GetAsync();
        _sut.Invalidate();
        _sut.Invalidate();
        _sut.Invalidate();
        await _sut.GetAsync();

        await _dapr.Received(2).InvokeMethodAsync<PolicySnapshotResponse>(
            Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_falls_back_to_local_configuration_when_dapr_invocation_throws()
    {
        _dapr.InvokeMethodAsync<PolicySnapshotResponse>(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("config-policy unavailable"));

        var snapshot = await _sut.GetAsync();

        Assert.Equal("fallback-v0", snapshot.Version);
        Assert.Equal(100m, snapshot.Thresholds.CeilingUsd);
        Assert.Contains("FallbackVendor", snapshot.KnownVendors);
    }

    [Fact]
    public async Task GetAsync_does_not_cache_fallback_so_next_call_retries_config_policy()
    {
        var call = 0;
        _dapr.InvokeMethodAsync<PolicySnapshotResponse>(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref call) == 1)
                    throw new HttpRequestException("config-policy down");
                return Task.FromResult(RemoteSnapshot("policy-v9"));
            });

        var degraded = await _sut.GetAsync();
        var recovered = await _sut.GetAsync();

        Assert.Equal("fallback-v0", degraded.Version);
        Assert.Equal("policy-v9", recovered.Version);
    }

    [Fact]
    public async Task Concurrent_GetAsync_only_fetches_once_from_dapr()
    {
        var callCount = 0;
        _dapr.InvokeMethodAsync<PolicySnapshotResponse>(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Delay(25);
                return RemoteSnapshot("policy-v2");
            });

        var tasks = Enumerable.Range(0, 20).Select(_ => _sut.GetAsync()).ToArray();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, callCount);
        Assert.All(results, r => Assert.Equal("policy-v2", r.Version));
    }

    private sealed class StaticOptionsMonitor : IOptionsMonitor<PolicySnapshotOptions>
    {
        public StaticOptionsMonitor(PolicySnapshotOptions value) => CurrentValue = value;

        public PolicySnapshotOptions CurrentValue { get; }

        public PolicySnapshotOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<PolicySnapshotOptions, string?> listener) => null;
    }
}
