using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Approval.Infrastructure.Workflows;

/// <summary>
/// Small deterministic input to <see cref="Activities.PublishReviewStatusActivity"/> so the workflow can
/// stay serialization-stable (replay-friendly): only primitives and enums, no clocks.
/// </summary>
public sealed record ReviewStatusPublishRequest
{
    public string TrackingId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public ReviewSubState SubState { get; init; }
    public string? WhatWeStillNeed { get; init; }
}
