using System.Security.Claims;

namespace ApprovalFlow.Gateway.Auth;

internal static class ApproverIdentity
{
    public static string? TryResolve(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return string.IsNullOrWhiteSpace(id) ? null : id;
    }
}
