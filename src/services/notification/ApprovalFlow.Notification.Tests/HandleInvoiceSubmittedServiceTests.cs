using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Models;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Application.Services;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Notification.Tests;

public class HandleInvoiceSubmittedServiceTests
{
    private readonly ISubmissionStatusRepository _repo = Substitute.For<ISubmissionStatusRepository>();
    private readonly HandleInvoiceSubmittedService _sut;

    public HandleInvoiceSubmittedServiceTests()
    {
        _sut = new HandleInvoiceSubmittedService(_repo, NullLogger<HandleInvoiceSubmittedService>.Instance);
    }

    private static InvoiceSubmittedV1 Event(string trackingId = "TRK-1") => new()
    {
        TrackingId = trackingId,
        CorrelationId = "corr-1",
        OccurredAt = DateTimeOffset.UtcNow,
        Invoice = new Invoice { InvoiceNumber = "I-1", Vendor = "Vendor Co", Total = 100m, Currency = "USD", Category = ExpenseCategory.Saas }
    };

    [Fact]
    public async Task First_delivery_projects_received_and_saves()
    {
        _repo.GetByTrackingIdAsync("TRK-1").Returns((SubmissionStatus?)null);
        SubmissionStatus? added = null;
        await _repo.AddAsync(Arg.Do<SubmissionStatus>(s => added = s), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Event(), CancellationToken.None);

        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.NotNull(added);
        Assert.Equal("TRK-1", added!.TrackingId);
        Assert.Equal(LifecycleStatus.Received, added.CurrentStatus);
    }

    [Fact]
    public async Task Redelivery_is_a_no_op()
    {
        _repo.GetByTrackingIdAsync("TRK-1")
            .Returns(SubmissionStatus.CreateReceived("TRK-1", "corr-1", DateTimeOffset.UtcNow));

        await _sut.HandleAsync(Event(), CancellationToken.None);

        await _repo.DidNotReceive().AddAsync(Arg.Any<SubmissionStatus>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
