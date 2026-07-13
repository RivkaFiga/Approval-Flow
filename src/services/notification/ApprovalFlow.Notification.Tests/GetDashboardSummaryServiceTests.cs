using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Application.Services;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Notification.Tests;

public class GetDashboardSummaryServiceTests
{
    private readonly IDashboardRepository _repo = Substitute.For<IDashboardRepository>();
    private readonly GetDashboardSummaryService _sut;

    public GetDashboardSummaryServiceTests()
    {
        _sut = new GetDashboardSummaryService(_repo);
    }

    [Fact]
    public async Task Returns_zeroes_when_no_submissions_have_been_processed()
    {
        _repo.GetAggregateAsync().Returns(new DashboardAggregate(0, 0, 0, 0m, 0m));

        var result = await _sut.GetAsync();

        Assert.Equal(0, result.TotalProcessed);
        Assert.Equal(0, result.AutoApprovedCount);
        Assert.Equal(0, result.HumanApprovalCount);
        Assert.Equal(0.0, result.EscalationRate);
        Assert.Equal(0m, result.AutoApprovedAmountUsd);
        Assert.Equal(0m, result.HumanApprovedAmountUsd);
    }

    [Fact]
    public async Task Maps_aggregate_fields_to_response()
    {
        _repo.GetAggregateAsync().Returns(new DashboardAggregate(10, 7, 3, 1050m, 450m));

        var result = await _sut.GetAsync();

        Assert.Equal(10, result.TotalProcessed);
        Assert.Equal(7, result.AutoApprovedCount);
        Assert.Equal(3, result.HumanApprovalCount);
        Assert.Equal(1050m, result.AutoApprovedAmountUsd);
        Assert.Equal(450m, result.HumanApprovedAmountUsd);
    }

    [Fact]
    public async Task Computes_escalation_rate_as_human_fraction_of_total()
    {
        _repo.GetAggregateAsync().Returns(new DashboardAggregate(10, 7, 3, 0m, 0m));

        var result = await _sut.GetAsync();

        Assert.Equal(0.3, result.EscalationRate, precision: 10);
    }

    [Fact]
    public async Task Escalation_rate_is_zero_when_no_items_processed()
    {
        _repo.GetAggregateAsync().Returns(new DashboardAggregate(0, 0, 0, 0m, 0m));

        var result = await _sut.GetAsync();

        Assert.Equal(0.0, result.EscalationRate);
    }

    [Fact]
    public async Task Escalation_rate_is_one_when_all_items_go_to_human_review()
    {
        _repo.GetAggregateAsync().Returns(new DashboardAggregate(5, 0, 5, 0m, 750m));

        var result = await _sut.GetAsync();

        Assert.Equal(1.0, result.EscalationRate, precision: 10);
    }
}
