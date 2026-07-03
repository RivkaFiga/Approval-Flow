namespace ApprovalFlow.Contracts.Enums;

/// <summary>The single action an approver takes on a pending item (F5, §9).</summary>
public enum ApproverActionType
{
    Approve,
    Reject,
    SendBack
}
