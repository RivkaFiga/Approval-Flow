namespace ApprovalFlow.E2E;

internal sealed class E2ESettings
{
    public string GatewayBaseUrl       { get; init; } = "http://localhost:5100";
    public string NotificationBaseUrl  { get; init; } = "http://localhost:5106";
    public int    HealthTimeoutSeconds  { get; init; } = 30;
    public int    FlowTimeoutSeconds    { get; init; } = 60;
    public int    PollIntervalMs        { get; init; } = 500;
    public JwtSettings Jwt              { get; init; } = new();
}

internal sealed class JwtSettings
{
    public string Issuer     { get; init; } = "approvalflow-dev";
    public string Audience   { get; init; } = "approvalflow-gateway";
    public string SigningKey  { get; init; } = "development-signing-key-please-override-in-production-32bytes+";
}
