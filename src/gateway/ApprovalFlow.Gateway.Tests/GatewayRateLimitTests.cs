using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Dapr.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Gateway.Tests;

/// <summary>
/// Verifies that the global rate limiter returns HTTP 429 once the configured permit limit is
/// exhausted, and that different authenticated users have independent partitions.
///
/// Uses PostConfigure to override the limiter after Program.cs registers it, because
/// Program.cs captures the permit-limit value from IConfiguration at startup (before the
/// WebApplicationFactory's ConfigureAppConfiguration runs).
/// </summary>
public sealed class GatewayRateLimitTests : IClassFixture<LowLimitGatewayFactory>
{
    private readonly LowLimitGatewayFactory _factory;

    public GatewayRateLimitTests(LowLimitGatewayFactory factory) => _factory = factory;

    [Fact]
    public async Task Exceeding_PermitLimit_Returns_429()
    {
        var client = _factory.CreateClientWithToken(sub: "rl-u1", roles: "approver");

        for (var i = 0; i < LowLimitGatewayFactory.PermitLimit; i++)
            (await client.GetAsync("/approvals/queue")).Dispose();

        var overLimit = await client.GetAsync("/approvals/queue");

        Assert.Equal(HttpStatusCode.TooManyRequests, overLimit.StatusCode);
    }

    [Fact]
    public async Task Different_Users_Have_Independent_Partitions()
    {
        var clientA = _factory.CreateClientWithToken(sub: "rl-u2", roles: "approver");
        var clientB = _factory.CreateClientWithToken(sub: "rl-u3", roles: "approver");

        // exhaust partition for user A
        for (var i = 0; i < LowLimitGatewayFactory.PermitLimit; i++)
            (await clientA.GetAsync("/approvals/queue")).Dispose();

        // A should now be throttled
        var overLimitA = await clientA.GetAsync("/approvals/queue");
        Assert.Equal(HttpStatusCode.TooManyRequests, overLimitA.StatusCode);

        // B has an independent partition and is still under the limit
        var responseB = await clientB.GetAsync("/approvals/queue");
        Assert.NotEqual(HttpStatusCode.TooManyRequests, responseB.StatusCode);
    }

    [Fact]
    public async Task Health_Endpoint_Is_Exempt_From_Rate_Limiting()
    {
        var client = _factory.CreateClient();

        // send well beyond the permit limit — health must never be blocked
        for (var i = 0; i < LowLimitGatewayFactory.PermitLimit + 5; i++)
            (await client.GetAsync("/healthz")).Dispose();

        var response = await client.GetAsync("/healthz");
        Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}

public sealed class LowLimitGatewayFactory : WebApplicationFactory<Program>
{
    public const int PermitLimit = 3;

    public DaprClient Dapr { get; } = Substitute.For<DaprClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DaprClient>();
            services.AddSingleton(Dapr);

            // Program.cs captures the permit-limit from IConfiguration as a local variable, so
            // ConfigureAppConfiguration overrides arrive too late.  PostConfigure<RateLimiterOptions>
            // runs after all Configure<RateLimiterOptions> calls (including the one inside
            // Program.cs's AddRateLimiter), making it the definitive value.
            services.PostConfigure<RateLimiterOptions>(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    var path = ctx.Request.Path.Value ?? string.Empty;
                    if (path.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase)
                        || path.StartsWith("/readyz",  StringComparison.OrdinalIgnoreCase)
                        || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
                        return RateLimitPartition.GetNoLimiter<string>("exempt");

                    var key = ctx.User.FindFirstValue("sub")
                        ?? ctx.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = PermitLimit,
                        Window               = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 0
                    });
                });
            });
        });
    }

    public HttpClient CreateClientWithToken(string sub, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokens.Issue(sub, roles));
        return client;
    }
}
