namespace ApprovalFlow.E2E;

internal sealed class E2ESettings
{
    public string GatewayBaseUrl      { get; init; } = "http://localhost:5100";
    public string NotificationBaseUrl { get; init; } = "http://localhost:5106";
    public int    HealthTimeoutSeconds { get; init; } = 30;
    public int    FlowTimeoutSeconds   { get; init; } = 60;
    public int    PollIntervalMs       { get; init; } = 500;
}
