using System.Security.Claims;
using ApprovalFlow.Gateway.Auth;
using Xunit;

namespace ApprovalFlow.Gateway.Tests;

public sealed class ApproverIdentityTests
{
    [Fact]
    public void TryResolve_ReturnsSubClaim_WhenPresent()
    {
        var user = PrincipalWith(("sub", "user-123"));

        Assert.Equal("user-123", ApproverIdentity.TryResolve(user));
    }

    [Fact]
    public void TryResolve_PrefersSub_OverNameIdentifier()
    {
        var user = PrincipalWith(
            ("sub", "sub-wins"),
            (ClaimTypes.NameIdentifier, "nid-loses"));

        Assert.Equal("sub-wins", ApproverIdentity.TryResolve(user));
    }

    [Fact]
    public void TryResolve_FallsBackToNameIdentifier_WhenSubMissing()
    {
        var user = PrincipalWith((ClaimTypes.NameIdentifier, "user-nid"));

        Assert.Equal("user-nid", ApproverIdentity.TryResolve(user));
    }

    [Fact]
    public void TryResolve_ReturnsNull_WhenNoSubjectClaim()
    {
        var user = PrincipalWith(("role", "approver"));

        Assert.Null(ApproverIdentity.TryResolve(user));
    }

    [Fact]
    public void TryResolve_ReturnsNull_WhenSubjectIsWhitespace()
    {
        var user = PrincipalWith(("sub", "   "));

        Assert.Null(ApproverIdentity.TryResolve(user));
    }

    [Fact]
    public void TryResolve_ReturnsNull_ForAnonymousPrincipal()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Null(ApproverIdentity.TryResolve(user));
    }

    private static ClaimsPrincipal PrincipalWith(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(authenticationType: "test");
        foreach (var (type, value) in claims)
            identity.AddClaim(new Claim(type, value));
        return new ClaimsPrincipal(identity);
    }
}
