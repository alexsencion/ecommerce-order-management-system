using ECommerce.Application.DTOs.Request;
using ECommerce.Application.Services;
using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces.Repositories;
using ECommerce.Domain.ReadModels;
using ECommerce.UnitTests.Common;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.UnitTests.Services
{
    public class ReportServiceTests : BaseTest
    {
        private static readonly DateTimeOffset Now =
            new(2026, 10, 10, 15, 0, 0, TimeSpan.Zero);

        private readonly Mock<IReportRepository> _reportRepo = new();
        private readonly ReportService _sut;

        public ReportServiceTests() : base()
        {
            _reportRepo.Setup(r => r.GetSalesTotalsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                        It.IsAny<IReadOnlyCollection<OrderStatus>>()))
                .ReturnsAsync(new SalesTotals(0, 0m, 0m));
            _reportRepo.Setup(r => r.GetDailySalesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                        It.IsAny<IReadOnlyCollection<OrderStatus>>()))
                .ReturnsAsync(new List<DailySales>());
            _reportRepo.Setup(r => r.GetTopProductsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                        It.IsAny<IReadOnlyCollection<OrderStatus>>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ProductSales>());
            _reportRepo.Setup(r => r.GetStatusCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<StatusCount>());

            _sut = new ReportService(_reportRepo.Object, new FixedTimeProvider(Now));
        }

        [Fact]
        public async Task NoDates_DefaultsToLast30DaysEndingToday()
        {
            var result = await _sut.GetDashboardAsync(new ReportQueryParams());

            result.IsSuccess.Should().BeTrue();
            result.Value!.From.Should().Be(new DateOnly(2026, 9, 11));
            result.Value.To.Should().Be(new DateOnly(2026, 10, 10));
            result.Value.DailySales.Should().HaveCount(30);

            _reportRepo.Verify(r => r.GetSalesTotalsAsync(
                new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<IReadOnlyCollection<OrderStatus>>()), Times.Once);
        }

        [Fact]
        public async Task RevenueQueries_UsePaidStatusesOnly()
        {
            await _sut.GetDashboardAsync(new ReportQueryParams());

            _reportRepo.Verify(r => r.GetSalesTotalsAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), 
                It.Is<IReadOnlyCollection<OrderStatus>>(s =>
                    s.SequenceEqual(OrderStatusGroups.Paid))), Times.Once);
        }

        [Fact]
        public async Task FromAfterTo_Returns400WithoutQuerying()
        {
            var result = await _sut.GetDashboardAsync(new ReportQueryParams
            {
                From = new DateOnly(2026, 10, 5),
                To = new DateOnly(2026, 10, 1)
            });

            result.StatusCode.Should().Be(400);
            _reportRepo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RangeOver366Days_Returns400()
        {
            var result = await _sut.GetDashboardAsync(new ReportQueryParams
            {
                From = new DateOnly(2025, 1, 1),
                To = new DateOnly(2026, 10, 10),
            });

            result.StatusCode.Should().Be(400);
            result.Error.Should().Contain("366");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(51)]
        public async Task TopOutOfRange_Returns400(int top)
        {
            var result = await _sut.GetDashboardAsync(new ReportQueryParams { Top = top });
            result.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Summary_ComputesNetRevenueAndAverageOrderValue()
        {
            _reportRepo.Setup(r => r.GetSalesTotalsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                           It.IsAny<IReadOnlyCollection<OrderStatus>>()))
                        .ReturnsAsync(new SalesTotals(4, 472m, 72m));

            var result = await _sut.GetDashboardAsync(new ReportQueryParams());

            var s = result.Value!.Summary;
            s.OrderCount.Should().Be(4);
            s.GrossRevenue.Should().Be(472m);
            s.TaxCollected.Should().Be(72m);
            s.NetRevenue.Should().Be(400);
            s.AverageOrderValue.Should().Be(118m);
        }

        [Fact]
        public async Task Summary_NoOrders_AverageIsZeroNotDivideByZero()
        {
            var result = await _sut.GetDashboardAsync(new ReportQueryParams());
            result.Value!.Summary.AverageOrderValue.Should().Be(0m);
        }

        [Fact]
        public async Task DailySales_FillsMissingDaysWithZeros()
        {
            _reportRepo.Setup(r => r.GetDailySalesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                          It.IsAny<IReadOnlyCollection<OrderStatus>>()))
                        .ReturnsAsync(new List<DailySales>
                        {
                            new(new DateTime(2026, 10, 2), 2, 236m)
                        });

            var result = await _sut.GetDashboardAsync(new ReportQueryParams
            {
                From = new DateOnly(2026, 10, 1),
                To = new DateOnly(2026, 10, 3)
            });

            var points = result.Value!.DailySales;
            points.Select(p => p.Date).Should().Equal(
                new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 3));
            points.Select(p => p.OrderCount).Should().Equal(0, 2, 0);
            points[1].Revenue.Should().Be(236m);
        }

        [Fact]
        public async Task StatusBreakdown_IncludesEveryStatusEvenAtZero()
        {
            _reportRepo.Setup(r => r.GetStatusCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                       .ReturnsAsync(new List<StatusCount>
                       {
                           new(OrderStatus.Pending, 3),
                           new(OrderStatus.Delivered, 1)
                       });

            var result = await _sut.GetDashboardAsync(new ReportQueryParams());

            var breakdown = result.Value!.StatusBreakdown;
            breakdown.Should().HaveCount(Enum.GetValues<OrderStatus>().Length);
            breakdown.Single(b => b.Status == OrderStatus.Pending).Count.Should().Be(3);
            breakdown.Single(b => b.Status == OrderStatus.Shipped).Count.Should().Be(0);
        }
    }
}
