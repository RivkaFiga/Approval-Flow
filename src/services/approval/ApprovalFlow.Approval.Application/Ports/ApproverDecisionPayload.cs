using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Approval.Application.Ports;

/// <summary>
/// Payload delivered to the durable workflow via <c>RaiseEvent("ApprovalDecision")</c> (§9). Framework-free
/// so it lives in Application; the Dapr-facing raiser adapter serializes it as the external event value.
/// </summary>
public sealed record ApproverDecisionPayload
{
    public ApproverActionType Action { get; init; }
    public string ApproverId { get; init; } = string.Empty;
    public string? Comment { get; init; }
}
