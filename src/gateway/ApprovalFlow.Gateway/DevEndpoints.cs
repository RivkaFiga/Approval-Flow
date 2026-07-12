using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ApprovalFlow.Gateway;

internal static class DevEndpoints
{
    internal static void MapDevTokenEndpoint(
        this WebApplication app,
        string issuer,
        string audience,
        byte[] signingKeyBytes)
    {
        app.MapPost("/dev/token", (DevTokenRequest req) =>
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrWhiteSpace(req.Sub))
                claims.Add(new Claim("sub", req.Sub));
            foreach (var role in req.Roles ?? [])
                claims.Add(new Claim("role", role));

            var key   = new SymmetricSecurityKey(signingKeyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer:            issuer,
                audience:          audience,
                claims:            claims,
                notBefore:         DateTime.UtcNow,
                expires:           DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        })
        .AllowAnonymous()
        .WithTags("Development")
        .WithSummary("Generate a signed JWT for local Swagger testing (Development only).");
    }
}

internal sealed record DevTokenRequest(string Sub, string[]? Roles);
