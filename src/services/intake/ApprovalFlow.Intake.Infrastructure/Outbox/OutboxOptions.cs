namespace ApprovalFlow.Intake.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollingIntervalMs { get; set; } = 1000;
    public int BatchSize { get; set; } = 50;
    public int MaxAttempts { get; set; } = 10;
}
