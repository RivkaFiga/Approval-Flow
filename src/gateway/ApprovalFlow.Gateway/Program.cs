using System.Security.Claims;
using System.Text;
using ApprovalFlow.Gateway;
using System.Threading.RateLimiting;
using ApprovalFlow.ServiceDefaults.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Gateway");

// Extend the Swagger UI registered by ServiceDefaults with JWT Bearer support.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Paste a JWT token. In Development, use POST /dev/token to generate one."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddDaprClient();

var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is required.");
var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is required.");
var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is required.");
var signingKeyBytes = Encoding.UTF8.GetBytes(signingKey);
if (signingKeyBytes.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes (256 bits) for HS256.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Submitter",     p => p.RequireAuthenticatedUser().RequireRole("submitter", "admin"));
    options.AddPolicy("Approver",      p => p.RequireAuthenticatedUser().RequireRole("approver", "admin"));
    options.AddPolicy("Admin",         p => p.RequireAuthenticatedUser().RequireRole("admin"));
    options.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());
});

// Rate limiting — global fixed-window keyed by authenticated user sub or remote IP.
// Health, readiness and Swagger endpoints are exempt so infrastructure probes are never blocked.
var rateLimitSection = builder.Configuration.GetSection("RateLimit");
var permitLimit  = rateLimitSection.GetValue<int>("PermitLimit",  100);
var windowSeconds = rateLimitSection.GetValue<int>("WindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
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
            PermitLimit               = permitLimit,
            Window                    = TimeSpan.FromSeconds(windowSeconds),
            QueueProcessingOrder      = QueueProcessingOrder.OldestFirst,
            QueueLimit                = 0
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = windowSeconds.ToString();
        await context.HttpContext.Response.WriteAsync("Too many requests.", token);
    };
});

var app = builder.Build();

app.UseServiceDefaults();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

if (app.Environment.IsDevelopment())
    app.MapDevTokenEndpoint(issuer, audience, signingKeyBytes);

app.Run();

public partial class Program;
