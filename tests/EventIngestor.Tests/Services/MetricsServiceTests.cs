using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.EventIngestor.Services;
using OrderAuditTrail.EventIngestor.Configuration;
using OrderAuditTrail.Shared.Configuration;
using OrderAuditTrail.Shared.Data;
using OrderAuditTrail.Shared.Data.Repositories;
using OrderAuditTrail.Shared.Events.Orders;
using OrderAuditTrail.Shared.Models;
using FluentAssertions;
using AutoFixture;
using Moq;
using System.Text.Json;

namespace OrderAuditTrail.EventIngestor.Tests.Services;

public class MetricsServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly IFixture _autoFixture;

    public MetricsServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
    }

    [Fact]
    public async Task RecordEventProcessedAsync_ValidEvent_SavesMetric()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MetricsService>>();
        var metricsRepository = new MetricsRepository(dbContext);
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(metricsRepository, metricsCollector, logger);
        
        var eventType = typeof(OrderCreatedEvent).Name;
        var topic = "orders.order.created";
        var processingTimeMs = 150;

        // Act
        await service.RecordEventProcessedAsync(eventType, topic, processingTimeMs);

        // Assert
        var metric = await dbContext.Metrics
            .FirstOrDefaultAsync(m => m.EventType == eventType && m.Topic == topic);
        
        metric.Should().NotBeNull();
        metric!.EventType.Should().Be(eventType);
        metric.Topic.Should().Be(topic);
        metric.ProcessingTimeMs.Should().Be(processingTimeMs);
        metric.Status.Should().Be("Success");
    }

    [Fact]
    public async Task RecordEventProcessedAsync_SetsTimestamp()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MetricsService>>();
        var metricsRepository = new MetricsRepository(dbContext);
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(metricsRepository, metricsCollector, logger);
        
        var beforeRecord = DateTime.UtcNow;

        // Act
        await service.RecordEventProcessedAsync("TestEvent", "test.topic", 100);

        // Assert
        var metric = await dbContext.Metrics
            .FirstOrDefaultAsync();
        
        metric.Should().NotBeNull();
        metric!.Timestamp.Should().BeCloseTo(beforeRecord, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task RecordEventProcessedAsync_EmptyOrNullEventType_ThrowsException(string? eventType)
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MetricsService>>();
        var metricsRepository = new MetricsRepository(dbContext);
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(metricsRepository, metricsCollector, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordEventProcessedAsync(eventType!, "test.topic", 100));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task RecordEventProcessedAsync_EmptyOrNullTopic_ThrowsException(string? topic)
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MetricsService>>();
        var metricsRepository = new MetricsRepository(dbContext);
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(metricsRepository, metricsCollector, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordEventProcessedAsync("TestEvent", topic!, 100));
    }

    [Fact]
    public async Task RecordEventProcessedAsync_NegativeProcessingTime_ThrowsException()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MetricsService>>();
        var metricsRepository = new MetricsRepository(dbContext);
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(metricsRepository, metricsCollector, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordEventProcessedAsync("TestEvent", "test.topic", -100));
    }

    [Fact]
    public async Task RecordEventFailedAsync_ValidEvent_SavesFailureMetric()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MetricsService>>();
        var metricsRepository = new MetricsRepository(dbContext);
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(metricsRepository, metricsCollector, logger);
        
        var eventType = typeof(OrderCreatedEvent).Name;
        var topic = "orders.order.created";
        var error = "Validation failed";

        // Act
        await service.RecordEventFailedAsync(eventType, topic, error);

        // Assert
        var metric = await dbContext.Metrics
            .FirstOrDefaultAsync(m => m.EventType == eventType && m.Topic == topic);
        
        metric.Should().NotBeNull();
        metric!.EventType.Should().Be(eventType);
        metric.Topic.Should().Be(topic);
        metric.Status.Should().Be("Failed");
        metric.ErrorMessage.Should().Be(error);
    }

    [Fact]
    public async Task RecordEventFailedAsync_SetsProcessingTimeToZero()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MetricsService>>();
        var metricsRepository = new MetricsRepository(dbContext);
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(metricsRepository, metricsCollector, logger);

        // Act
        await service.RecordEventFailedAsync("TestEvent", "test.topic", "Error");

        // Assert
        var metric = await dbContext.Metrics
            .FirstOrDefaultAsync();
        
        metric.Should().NotBeNull();
        metric!.ProcessingTimeMs.Should().Be(0);
    }

    [Fact]
    public async Task RecordEventFailedAsync_DatabaseError_ThrowsException()
    {
        // Arrange
        var logger = Mock.Of<ILogger<MetricsService>>();
        var mockMetricsRepository = new Mock<IMetricsRepository>();
        var metricsCollector = new MetricsCollector();
        
        mockMetricsRepository
            .Setup(r => r.AddAsync(It.IsAny<Metric>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        
        var service = new MetricsService(mockMetricsRepository.Object, metricsCollector, logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordEventFailedAsync("TestEvent", "test.topic", "Error"));
    }

    [Fact]
    public async Task RecordEventProcessedAsync_CallsRepositoryAddAsync()
    {
        // Arrange
        var logger = Mock.Of<ILogger<MetricsService>>();
        var mockMetricsRepository = new Mock<IMetricsRepository>();
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(mockMetricsRepository.Object, metricsCollector, logger);

        // Act
        await service.RecordEventProcessedAsync("TestEvent", "test.topic", 100);

        // Assert
        mockMetricsRepository.Verify(r => r.AddAsync(It.IsAny<Metric>()), Times.Once);
    }

    [Fact]
    public async Task RecordEventFailedAsync_CallsRepositoryAddAsync()
    {
        // Arrange
        var logger = Mock.Of<ILogger<MetricsService>>();
        var mockMetricsRepository = new Mock<IMetricsRepository>();
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(mockMetricsRepository.Object, metricsCollector, logger);

        // Act
        await service.RecordEventFailedAsync("TestEvent", "test.topic", "Error");

        // Assert
        mockMetricsRepository.Verify(r => r.AddAsync(It.IsAny<Metric>()), Times.Once);
    }

    [Fact]
    public void RecordEventProcessedAsync_UpdatesMetricsCollector()
    {
        // Arrange
        var logger = Mock.Of<ILogger<MetricsService>>();
        var mockMetricsRepository = new Mock<IMetricsRepository>();
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(mockMetricsRepository.Object, metricsCollector, logger);

        var initialProcessedCount = metricsCollector.ProcessedEventsCount;

        // Act
        service.RecordEventProcessedAsync("TestEvent", "test.topic", 100);

        // Assert
        metricsCollector.ProcessedEventsCount.Should().Be(initialProcessedCount + 1);
    }

    [Fact]
    public void RecordEventFailedAsync_UpdatesMetricsCollector()
    {
        // Arrange
        var logger = Mock.Of<ILogger<MetricsService>>();
        var mockMetricsRepository = new Mock<IMetricsRepository>();
        var metricsCollector = new MetricsCollector();
        var service = new MetricsService(mockMetricsRepository.Object, metricsCollector, logger);

        var initialFailedCount = metricsCollector.FailedEventsCount;

        // Act
        service.RecordEventFailedAsync("TestEvent", "test.topic", "Error");

        // Assert
        metricsCollector.FailedEventsCount.Should().Be(initialFailedCount + 1);
    }
}
