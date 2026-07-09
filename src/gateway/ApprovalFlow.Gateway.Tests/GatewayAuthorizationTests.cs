using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Dapr.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Gateway.Tests;

public sealed class GatewayAuthorizationTests : IClassFixture<GatewayFactory>
{
    private readonly GatewayFactory _factory;

    public GatewayAuthorizationTests(GatewayFactory factory) => _factory = factory;

    // ---------- Anonymous requests are rejected on every protected endpoint ----------

    public static IEnumerable<object[]> ProtectedEndpoints => new[]
    {
        new object[] { HttpMethod.Post, "/api/intake",                    true  },
        new object[] { HttpMethod.Get,  "/api/status/abc",                false },
        new object[] { HttpMethod.Get,  "/approvals/queue",               false },
        new object[] { HttpMethod.Post, "/approvals/abc/approve",         true  },
        new object[] { HttpMethod.Post, "/approvals/abc/reject",          true  },
        new object[] { HttpMethod.Post, "/approvals/abc/request-info",    true  },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Unauthenticated_Request_Returns_401(HttpMethod method, string path, bool sendBody)
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(method, path);
        if (sendBody)
            request.Content = JsonContent.Create(new { });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Wrong role is rejected with 403 ----------

    [Fact]
    public async Task Submitter_Cannot_Access_Approvals_Queue()
    {
        var client = CreateClientWithToken(sub: "u1", roles: "submitter");

        var response = await client.GetAsync("/approvals/queue");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approver_Cannot_Submit_Invoice()
    {
        var client = CreateClientWithToken(sub: "u1", roles: "approver");

        var response = await client.PostAsJsonAsync("/api/intake", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Token_Without_Role_Is_Rejected_From_Protected_Route()
    {
        var client = CreateClientWithToken(sub: "u1"); // no roles

        var response = await client.GetAsync("/approvals/queue");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Fail-closed: approver token without a subject claim is refused ----------

    [Fact]
    public async Task Approver_Without_Sub_Claim_Is_Refused_With_401()
    {
        var client = CreateClientWithToken(sub: null, roles: "approver");

        var response = await client.PostAsJsonAsync(
            "/approvals/T-1/approve", new { approverId = "ignored", comment = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Signature / issuer / expiry rejections ----------

    [Fact]
    public async Task Token_Signed_With_Wrong_Key_Is_Rejected()
    {
        var token = IssueSignedToken(
            key: "a-completely-different-key-that-is-also-32-bytes-long!",
            issuer: JwtTestTokens.Issuer,
            audience: JwtTestTokens.Audience,
            expires: DateTime.UtcNow.AddMinutes(5),
            claims: [new("sub", "u1"), new("role", "approver")]);

        var response = await SendWithBearer(token, HttpMethod.Get, "/approvals/queue");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_With_Wrong_Issuer_Is_Rejected()
    {
        var token = IssueSignedToken(
            key: JwtTestTokens.SigningKey,
            issuer: "some-other-issuer",
            audience: JwtTestTokens.Audience,
            expires: DateTime.UtcNow.AddMinutes(5),
            claims: [new("sub", "u1"), new("role", "approver")]);

        var response = await SendWithBearer(token, HttpMethod.Get, "/approvals/queue");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_With_Wrong_Audience_Is_Rejected()
    {
        var token = IssueSignedToken(
            key: JwtTestTokens.SigningKey,
            issuer: JwtTestTokens.Issuer,
            audience: "some-other-audience",
            expires: DateTime.UtcNow.AddMinutes(5),
            claims: [new("sub", "u1"), new("role", "approver")]);

        var response = await SendWithBearer(token, HttpMethod.Get, "/approvals/queue");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Expired_Token_Is_Rejected()
    {
        var token = IssueSignedToken(
            key: JwtTestTokens.SigningKey,
            issuer: JwtTestTokens.Issuer,
            audience: JwtTestTokens.Audience,
            expires: DateTime.UtcNow.AddHours(-1),
            claims: [new("sub", "u1"), new("role", "approver")]);

        var response = await SendWithBearer(token, HttpMethod.Get, "/approvals/queue");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Health endpoint stays anonymous by design ----------

    [Fact]
    public async Task Health_Endpoint_Is_Anonymous()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/healthz");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Helpers ----------

    private HttpClient CreateClientWithToken(string? sub, params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokens.Issue(sub, roles));
        return client;
    }

    private async Task<HttpResponseMessage> SendWithBearer(string token, HttpMethod method, string path)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(new HttpRequestMessage(method, path));
    }

    private static string IssueSignedToken(
        string key, string issuer, string audience, DateTime expires, IEnumerable<Claim> claims)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: expires.AddHours(-1),
            expires: expires,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

public sealed class GatewayFactory : WebApplicationFactory<Program>
{
    public DaprClient Dapr { get; } = Substitute.For<DaprClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DaprClient>();
            services.AddSingleton(Dapr);
        });
    }
}
