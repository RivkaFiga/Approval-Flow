using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApprovalFlow.E2E.Helpers;

internal static class JwtTokenHelper
{
    public static string CreateToken(string sub, string[] roles, JwtSettings settings)
    {
        var claims = new List<Claim>
        {
            new("sub", sub)
        };

        claims.AddRange(
            roles.Select(r => new Claim("role", r)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(settings.SigningKey));

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public static string CreateSubmitterToken(JwtSettings settings) =>
        CreateToken(
            "e2e-submitter",
            ["submitter"],
            settings);

    public static string CreateApproverToken(JwtSettings settings) =>
        CreateToken(
            "e2e-approver",
            ["approver"],
            settings);

    public static string CreateAdminToken(JwtSettings settings) =>
        CreateToken(
            "e2e-admin",
            ["admin"],
            settings);
}