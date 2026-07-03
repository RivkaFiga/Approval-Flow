using ApprovalFlow.Contracts.Events.V1;

namespace ApprovalFlow.Intake.Application.Ports;

public interface IIntakeEventPublisher
{
    Task PublishInvoiceSubmittedAsync(InvoiceSubmittedV1 @event, CancellationToken ct = default);
}
