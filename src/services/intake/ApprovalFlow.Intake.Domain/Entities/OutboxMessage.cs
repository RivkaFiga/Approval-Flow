namespace ApprovalFlow.Intake.Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(
        string eventType,
        string payload,
        DateTimeOffset occurredAt,
        string? correlationId)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            OccurredAt = occurredAt,
            CorrelationId = correlationId,
            DispatchedAt = null,
            AttemptCount = 0,
            LastError = null
        };
    }

    public void MarkDispatched(DateTimeOffset dispatchedAt)
    {
        DispatchedAt = dispatchedAt;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        AttemptCount += 1;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }
}
