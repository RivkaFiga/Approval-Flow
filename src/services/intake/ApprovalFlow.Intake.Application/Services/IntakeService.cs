using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;
using ApprovalFlow.Intake.Application.Ports;
using ApprovalFlow.Intake.Application.Validation;
using ApprovalFlow.Intake.Domain.Entities;
using ApprovalFlow.Intake.Domain.Exceptions;

namespace ApprovalFlow.Intake.Application.Services;

public sealed class IntakeService
{
    private readonly ISubmittedInvoiceRepository _repo;
    private readonly IIntakeEventPublisher _publisher;

    public IntakeService(ISubmittedInvoiceRepository repo, IIntakeEventPublisher publisher)
    {
        _repo = repo;
        _publisher = publisher;
    }

    public async Task<SubmitInvoiceResponse> SubmitAsync(
        SubmitInvoiceRequest request,
        string correlationId,
        CancellationToken ct = default)
    {
        var errors = InvoiceValidator.Validate(request.Invoice);
        if (errors.Count > 0)
            throw new InvoiceValidationException(errors);

        var dedupKey = ComputeDedupKey(request.Invoice);
        var trackingId = GenerateTrackingId();

        var isDuplicate = await _repo.ExistsByDedupKeyAsync(dedupKey, ct);
        if (isDuplicate)
        {
            return new SubmitInvoiceResponse
            {
                TrackingId = trackingId,
                Status = AcceptanceStatus.Duplicate
            };
        }

        var lineItemsJson = JsonSerializer.Serialize(request.Invoice.LineItems);

        var entity = SubmittedInvoice.Create(
            trackingId,
            correlationId,
            request.Invoice.InvoiceNumber,
            request.Invoice.Vendor,
            request.Invoice.VendorKnown,
            request.Invoice.Submitter,
            request.Invoice.Department,
            (int)request.Invoice.Category,
            request.Invoice.Currency,
            request.Invoice.TaxAmount,
            request.Invoice.Total,
            request.Invoice.ReceiptPresent,
            request.Invoice.Attendees,
            request.Invoice.Date,
            request.Invoice.Notes,
            lineItemsJson,
            dedupKey);

        await _repo.AddAsync(entity, ct);

        var @event = new InvoiceSubmittedV1
        {
            TrackingId = trackingId,
            Invoice = request.Invoice,
            CorrelationId = correlationId,
            OccurredAt = DateTimeOffset.UtcNow
        };

        // Publisher stages the event on the same DbContext; SaveChanges commits invoice + outbox atomically.
        await _publisher.PublishInvoiceSubmittedAsync(@event, ct);
        await _repo.SaveChangesAsync(ct);

        return new SubmitInvoiceResponse
        {
            TrackingId = trackingId,
            Status = AcceptanceStatus.Accepted
        };
    }

    private static string GenerateTrackingId()
    {
        return $"TRK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    private static string ComputeDedupKey(Invoice invoice)
    {
        var raw = $"{invoice.InvoiceNumber}|{invoice.Vendor}|{invoice.Total}|{invoice.Date:yyyy-MM-dd}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
