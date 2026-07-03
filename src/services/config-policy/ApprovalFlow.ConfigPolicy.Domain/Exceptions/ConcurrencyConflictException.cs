namespace ApprovalFlow.ConfigPolicy.Domain.Exceptions;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Guid id)
        : base($"Policy document '{id}' was modified by another request. Retry with the current version.") { }
}
