using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Contracts.Events.V1;

/// <summary>
/// <c>review.status</c> — published by Approval/Workflow, consumed by Notification (§5.2). Carries HITL
/// sub-state transitions so F2 shows a live status during the slow 20% and send-back reaches the submitter.
/// </summary>
public sealed record ReviewStatusV1 : IntegrationEvent
{
    public override string Type => EventTypes.ReviewStatus;
    public override int SchemaVersion => 1;

    public string TrackingId { get; init; } = string.Empty;

    public ReviewSubState SubState { get; init; }

    /// <summary>For <c>awaiting-info</c> send-back: the plain-language "what we still need" (F5, §9).</summary>
    public string? WhatWeStillNeed { get; init; }
}
