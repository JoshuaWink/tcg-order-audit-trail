using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderAuditTrail.AuditApi.Controllers;
using OrderAuditTrail.AuditApi.Models;
using OrderAuditTrail.AuditApi.Services;
using FluentAssertions;
using AutoFixture;
using AutoFixture.Xunit2;
using Microsoft.AspNetCore.Http;

namespace OrderAuditTrail.AuditApi.Tests.Controllers;

public class MetricsControllerTests : IClassFixture<AuditApiTestFixture>
{
    private readonly AuditApiTestFixture _fixture;
    private readonly Fixture _autoFixture;
    private readonly Mock<IMetricsQueryService> _mockMetricsService;
    private readonly Mock<ILogger<MetricsController>> _mockLogger;
    private readonly MetricsController _controller;

    public MetricsControllerTests(AuditApiTestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
        _mockMetricsService = new Mock<IMetricsQueryService>();
        _mockLogger = new Mock<ILogger<MetricsController>>();
        
        _controller = new MetricsController(_mockMetricsService.Object, _mockLogger.Object);
        
        // Setup controller context for testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetSystemMetrics_ReturnsOkResult()
    {
        // Arrange
        var expectedMetrics = new SystemMetrics
        {
            TotalEvents = 1000,
            EventsLast24Hours = 100,
            EventsLast7Days = 500,
            EventsLast30Days = 900,
            UniqueOrdersLast24Hours = 50,
            UniqueOrdersLast7Days = 250,
            UniqueOrdersLast30Days = 450,
            AvgEventsPerOrder = 2.22,
            TopEventTypes = new[]
            {
                new EventTypeMetric { EventType = "OrderCreated", Count = 300 },
                new EventTypeMetric { EventType = "OrderUpdated", Count = 250 },
                new EventTypeMetric { EventType = "OrderCancelled", Count = 200 }
            }
        };

        _mockMetricsService
            .Setup(s => s.GetSystemMetricsAsync())
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetSystemMetrics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMetrics);
        
        _mockMetricsService.Verify(s => s.GetSystemMetricsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSystemMetrics_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        _mockMetricsService
            .Setup(s => s.GetSystemMetricsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetSystemMetrics();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetEventMetrics_WithValidDateRange_ReturnsOkResult()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var expectedMetrics = new EventMetrics
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEvents = 500,
            EventsByType = new Dictionary<string, int>
            {
                { "OrderCreated", 150 },
                { "OrderUpdated", 200 },
                { "OrderCancelled", 100 },
                { "OrderShipped", 50 }
            },
            EventsByHour = new Dictionary<int, int>
            {
                { 0, 10 }, { 1, 5 }, { 2, 8 }, { 3, 15 }
            },
            EventsByDay = new Dictionary<DateTime, int>
            {
                { startDate.Date, 70 },
                { startDate.Date.AddDays(1), 80 },
                { startDate.Date.AddDays(2), 75 }
            }
        };

        _mockMetricsService
            .Setup(s => s.GetEventMetricsAsync(startDate, endDate))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetEventMetrics(startDate, endDate);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMetrics);
        
        _mockMetricsService.Verify(s => s.GetEventMetricsAsync(startDate, endDate), Times.Once);
    }

    [Fact]
    public async Task GetEventMetrics_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-1); // End date before start date

        // Act
        var result = await _controller.GetEventMetrics(startDate, endDate);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result as BadRequestObjectResult;
        badResult!.Value.Should().NotBeNull();
        
        _mockMetricsService.Verify(s => s.GetEventMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GetEventMetrics_WithDateRangeTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-100); // More than 90 days
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetEventMetrics(startDate, endDate);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result as BadRequestObjectResult;
        badResult!.Value.Should().NotBeNull();
        
        _mockMetricsService.Verify(s => s.GetEventMetricsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderMetrics_WithValidOrderId_ReturnsOkResult()
    {
        // Arrange
        var orderId = "ORDER-123";
        var expectedMetrics = new OrderMetrics
        {
            OrderId = orderId,
            TotalEvents = 5,
            FirstEventDate = DateTime.UtcNow.AddDays(-5),
            LastEventDate = DateTime.UtcNow.AddDays(-1),
            EventTypes = new[] { "OrderCreated", "OrderUpdated", "OrderShipped" },
            EventTimeline = new[]
            {
                new EventTimelineItem
                {
                    EventType = "OrderCreated",
                    Timestamp = DateTime.UtcNow.AddDays(-5),
                    Description = "Order created"
                },
                new EventTimelineItem
                {
                    EventType = "OrderUpdated",
                    Timestamp = DateTime.UtcNow.AddDays(-3),
                    Description = "Order updated"
                }
            }
        };

        _mockMetricsService
            .Setup(s => s.GetOrderMetricsAsync(orderId))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetOrderMetrics(orderId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMetrics);
        
        _mockMetricsService.Verify(s => s.GetOrderMetricsAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetOrderMetrics_WithNonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var orderId = "ORDER-999";

        _mockMetricsService
            .Setup(s => s.GetOrderMetricsAsync(orderId))
            .ReturnsAsync((OrderMetrics?)null);

        // Act
        var result = await _controller.GetOrderMetrics(orderId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        
        _mockMetricsService.Verify(s => s.GetOrderMetricsAsync(orderId), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetOrderMetrics_WithInvalidOrderId_ReturnsBadRequest(string orderId)
    {
        // Act
        var result = await _controller.GetOrderMetrics(orderId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        _mockMetricsService.Verify(s => s.GetOrderMetricsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPerformanceMetrics_ReturnsOkResult()
    {
        // Arrange
        var expectedMetrics = new PerformanceMetrics
        {
            AvgQueryTimeMs = 150.5,
            MaxQueryTimeMs = 500.0,
            MinQueryTimeMs = 25.0,
            TotalQueries = 1000,
            QueriesPerSecond = 10.5,
            DatabaseConnectionPoolSize = 20,
            ActiveConnections = 15,
            CacheHitRate = 0.85,
            MemoryUsageMB = 512.0,
            CpuUsagePercent = 45.0
        };

        _mockMetricsService
            .Setup(s => s.GetPerformanceMetricsAsync())
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _controller.GetPerformanceMetrics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMetrics);
        
        _mockMetricsService.Verify(s => s.GetPerformanceMetricsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPerformanceMetrics_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        _mockMetricsService
            .Setup(s => s.GetPerformanceMetricsAsync())
            .ThrowsAsync(new Exception("Performance monitoring error"));

        // Act
        var result = await _controller.GetPerformanceMetrics();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}
