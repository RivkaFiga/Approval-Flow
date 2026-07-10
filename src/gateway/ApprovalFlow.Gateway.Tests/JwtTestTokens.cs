using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApprovalFlow.Gateway.Tests;

internal static class JwtTestTokens
{
    public const string Issuer = "approvalflow-dev";
    public const string Audience = "approvalflow-gateway";
    public const string SigningKey = "development-signing-key-please-override-in-production-32bytes+";

    public static string Issue(string? sub, params string[] roles)
    {
        var claims = new List<Claim>();
        if (sub is not null)
            claims.Add(new Claim("sub", sub));
        foreach (var role in roles)
            claims.Add(new Claim("role", role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
