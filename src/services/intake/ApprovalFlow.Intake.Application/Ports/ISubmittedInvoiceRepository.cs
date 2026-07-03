using ApprovalFlow.Intake.Domain.Entities;

namespace ApprovalFlow.Intake.Application.Ports;

public interface ISubmittedInvoiceRepository
{
    Task<bool> ExistsByDedupKeyAsync(string dedupKey, CancellationToken ct = default);
    Task AddAsync(SubmittedInvoice invoice, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
