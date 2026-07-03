namespace ApprovalFlow.Contracts.Enums;

/// <summary>Result of an async intake accept (§6). A duplicate is accepted but not re-processed (GLOBAL-DUP).</summary>
public enum AcceptanceStatus
{
    Accepted,
    Duplicate
}
