namespace ApprovalFlow.Intake.Domain.Exceptions;

public sealed class InvoiceValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public InvoiceValidationException(IReadOnlyList<string> errors)
        : base($"Invoice validation failed: {string.Join("; ", errors)}")
    {
        Errors = errors;
    }
}
