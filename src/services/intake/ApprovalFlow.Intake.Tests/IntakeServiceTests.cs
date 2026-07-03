using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;
using ApprovalFlow.Intake.Application.Ports;
using ApprovalFlow.Intake.Application.Services;
using ApprovalFlow.Intake.Domain.Entities;
using ApprovalFlow.Intake.Domain.Exceptions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Intake.Tests;

public class IntakeServiceTests
{
    private readonly ISubmittedInvoiceRepository _repo = Substitute.For<ISubmittedInvoiceRepository>();
    private readonly IIntakeEventPublisher _publisher = Substitute.For<IIntakeEventPublisher>();
    private readonly IntakeService _sut;

    public IntakeServiceTests()
    {
        _sut = new IntakeService(_repo, _publisher);
    }

    private static SubmitInvoiceRequest ValidRequest() => new()
    {
        Invoice = new Invoice
        {
            InvoiceNumber = "INV-001",
            Vendor = "Acme Corp",
            Submitter = "jane@example.com",
            Department = "Engineering",
            Category = ExpenseCategory.Saas,
            Currency = "USD",
            Total = 500m,
            TaxAmount = 50m,
            ReceiptPresent = true,
            Date = new DateOnly(2026, 7, 1),
            LineItems = new[] { new LineItem { Description = "License", Quantity = 1, UnitPrice = 500m } }
        }
    };

    [Fact]
    public async Task Submit_valid_invoice_returns_accepted_with_tracking_id()
    {
        _repo.ExistsByDedupKeyAsync(Arg.Any<string>()).Returns(false);

        var result = await _sut.SubmitAsync(ValidRequest(), "corr-123");

        Assert.Equal(AcceptanceStatus.Accepted, result.Status);
        Assert.StartsWith("TRK-", result.TrackingId);
        await _repo.Received(1).AddAsync(Arg.Any<SubmittedInvoice>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishInvoiceSubmittedAsync(Arg.Any<InvoiceSubmittedV1>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_duplicate_returns_duplicate_status_and_does_not_persist()
    {
        _repo.ExistsByDedupKeyAsync(Arg.Any<string>()).Returns(true);

        var result = await _sut.SubmitAsync(ValidRequest(), "corr-456");

        Assert.Equal(AcceptanceStatus.Duplicate, result.Status);
        await _repo.DidNotReceive().AddAsync(Arg.Any<SubmittedInvoice>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishInvoiceSubmittedAsync(Arg.Any<InvoiceSubmittedV1>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_invalid_invoice_throws_validation_exception()
    {
        var request = new SubmitInvoiceRequest
        {
            Invoice = new Invoice()
        };

        var ex = await Assert.ThrowsAsync<InvoiceValidationException>(
            () => _sut.SubmitAsync(request, "corr-789"));

        Assert.NotEmpty(ex.Errors);
    }

    [Fact]
    public async Task Published_event_contains_correct_correlation_id_and_tracking_id()
    {
        _repo.ExistsByDedupKeyAsync(Arg.Any<string>()).Returns(false);
        InvoiceSubmittedV1? capturedEvent = null;
        await _publisher.PublishInvoiceSubmittedAsync(
            Arg.Do<InvoiceSubmittedV1>(e => capturedEvent = e),
            Arg.Any<CancellationToken>());

        var result = await _sut.SubmitAsync(ValidRequest(), "corr-abc");

        Assert.NotNull(capturedEvent);
        Assert.Equal("corr-abc", capturedEvent!.CorrelationId);
        Assert.Equal(result.TrackingId, capturedEvent.TrackingId);
        Assert.Equal("INV-001", capturedEvent.Invoice.InvoiceNumber);
    }
}
