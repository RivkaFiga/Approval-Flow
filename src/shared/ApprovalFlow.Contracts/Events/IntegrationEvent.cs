namespace ApprovalFlow.Contracts.Events;

/// <summary>
/// Base for every pub/sub payload. Each message is a CloudEvent carrying a <see cref="Type"/> and a
/// <see cref="SchemaVersion"/> (§5.2); consumers ignore unknown fields so services stay independently
/// deployable (§13). Carries the internal end-to-end audit-join key <see cref="CorrelationId"/> (§12.1).
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Canonical CloudEvent <c>type</c>, e.g. <c>invoice.submitted</c>.</summary>
    public abstract string Type { get; }

    /// <summary>Payload schema version; bumped on a breaking change.</summary>
    public abstract int SchemaVersion { get; }

    /// <summary>Internal correlation id, issued once at the gateway and propagated on every message (§12.1).</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Wall-clock UTC time when the event occurred. Publishers must set this explicitly; there is no default.</summary>
    public DateTimeOffset OccurredAt { get; init; }
}
