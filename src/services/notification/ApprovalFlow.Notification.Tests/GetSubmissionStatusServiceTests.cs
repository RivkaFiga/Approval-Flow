using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Application.Services;
using ApprovalFlow.Notification.Domain.Entities;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Notification.Tests;

public class GetSubmissionStatusServiceTests
{
    private readonly ISubmissionStatusRepository _repo = Substitute.For<ISubmissionStatusRepository>();
    private readonly GetSubmissionStatusService _sut;

    public GetSubmissionStatusServiceTests()
    {
        _sut = new GetSubmissionStatusService(_repo);
    }

    [Fact]
    public async Task Unknown_tracking_id_returns_null()
    {
        _repo.GetByTrackingIdAsync("TRK-missing").Returns((SubmissionStatus?)null);

        var response = await _sut.GetAsync("TRK-missing");

        Assert.Null(response);
    }

    [Fact]
    public async Task Projects_the_current_status_to_the_shared_response_contract()
    {
        var t0 = new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);
        var status = SubmissionStatus.CreateReceived("TRK-1", "corr-1", t0);
        status.ApplyDecision(Route.HumanReview, 199.99m, t0.AddSeconds(1));
        _repo.GetByTrackingIdAsync("TRK-1").Returns(status);

        var response = await _sut.GetAsync("TRK-1");

        Assert.NotNull(response);
        Assert.Equal("TRK-1", response!.TrackingId);
        Assert.Equal(LifecycleStatus.UnderReview, response.Status);
        Assert.Equal(Route.HumanReview, response.Route);
        Assert.Equal(199.99m, response.AmountUsd);
    }
}
