using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Gateway.Tests;

public sealed class SwaggerConfigTests : IClassFixture<GatewayFactory>
{
    private readonly GatewayFactory _factory;

    public SwaggerConfigTests(GatewayFactory factory) => _factory = factory;

    // Swagger JSON must be reachable and declare the Bearer scheme so the UI shows "Authorize"
    [Fact]
    public async Task SwaggerJson_Returns_200_And_Contains_Bearer_SecurityScheme()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"Bearer\"", json);
        Assert.Contains("\"bearer\"", json); // scheme value (lowercase per HTTP spec)
    }

    // Dev endpoint must produce a non-empty token string in Development
    [Fact]
    public async Task DevToken_Returns_Valid_Token_In_Development()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/dev/token", new { sub = "swagger-user", roles = new[] { "submitter" } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(body?.Token);
        Assert.NotEmpty(body.Token);
        // A JWT has exactly 3 dot-separated segments
        Assert.Equal(3, body.Token.Split('.').Length);
    }

    // Token produced by /dev/token must be accepted by a protected endpoint (auth passes)
    [Fact]
    public async Task DevToken_Token_Is_Accepted_By_Protected_Endpoint()
    {
        var client = _factory.CreateClient();

        var tokenResponse = await client.PostAsJsonAsync(
            "/dev/token", new { sub = "swagger-user", roles = new[] { "approver" } });
        var body = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Token);
        var response = await client.GetAsync("/approvals/queue");

        // 401 = unauthenticated, 403 = wrong role — both mean auth failed.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Dev endpoint must not exist outside Development — route should not be registered
    [Fact]
    public async Task DevToken_Endpoint_Is_Absent_In_Production()
    {
        await using var factory = new ProductionGatewayFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/dev/token", new { sub = "user", roles = new[] { "submitter" } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record TokenResponse([property: JsonPropertyName("token")] string Token);
}

public sealed class ProductionGatewayFactory : WebApplicationFactory<Program>
{
    public DaprClient Dapr { get; } = Substitute.For<DaprClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DaprClient>();
            services.AddSingleton(Dapr);
        });
    }
}
