using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderAuditTrail.EventIngestor.Services;
using OrderAuditTrail.Shared.Configuration;
using OrderAuditTrail.Shared.Events.Orders;
using OrderAuditTrail.Shared.Events.Payments;
using OrderAuditTrail.Shared.Models;
using FluentAssertions;
using AutoFixture;
using Moq;
using System.Text.Json;

namespace OrderAuditTrail.EventIngestor.Tests.Services;

public class EventProcessorTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly IFixture _autoFixture;

    public EventProcessorTests(TestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
    }

    [Fact]
    public async Task ProcessEventAsync_ValidEvent_ProcessesSuccessfully()
    {
        // Arrange
        var mockValidationService = new Mock<IEventValidationService>();
        var mockPersistenceService = new Mock<IEventPersistenceService>();
        var mockMetricsService = new Mock<IMetricsService>();
        var mockDeadLetterService = new Mock<IDeadLetterQueueService>();
        var logger = Mock.Of<ILogger<EventProcessor>>();
        
        var processor = new EventProcessor(
            mockValidationService.Object,
            mockPersistenceService.Object,
            mockMetricsService.Object,
            mockDeadLetterService.Object,
            logger);

        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);
        var topic = "orders.order.created";

        mockValidationService
            .Setup(v => v.ValidateAsync(eventJson, typeof(OrderCreatedEvent)))
            .ReturnsAsync(true);

        // Act
        await processor.ProcessEventAsync(eventJson, typeof(OrderCreatedEvent), topic);

        // Assert
        mockValidationService.Verify(v => v.ValidateAsync(eventJson, typeof(OrderCreatedEvent)), Times.Once);
        mockPersistenceService.Verify(p => p.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, topic), Times.Once);
        mockMetricsService.Verify(m => m.RecordEventProcessedAsync(typeof(OrderCreatedEvent).Name, topic, It.IsAny<int>()), Times.Once);
        mockDeadLetterService.Verify(d => d.SendToDeadLetterQueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ProcessEventAsync_InvalidEvent_SendsToDeadLetterQueue()
    {
        // Arrange
        var mockValidationService = new Mock<IEventValidationService>();
        var mockPersistenceService = new Mock<IEventPersistenceService>();
        var mockMetricsService = new Mock<IMetricsService>();
        var mockDeadLetterService = new Mock<IDeadLetterQueueService>();
        var logger = Mock.Of<ILogger<EventProcessor>>();
        
        var processor = new EventProcessor(
            mockValidationService.Object,
            mockPersistenceService.Object,
            mockMetricsService.Object,
            mockDeadLetterService.Object,
            logger);

        var invalidEventJson = "{ invalid json }";
        var topic = "orders.order.created";

        mockValidationService
            .Setup(v => v.ValidateAsync(invalidEventJson, typeof(OrderCreatedEvent)))
            .ReturnsAsync(false);

        // Act
        await processor.ProcessEventAsync(invalidEventJson, typeof(OrderCreatedEvent), topic);

        // Assert
        mockValidationService.Verify(v => v.ValidateAsync(invalidEventJson, typeof(OrderCreatedEvent)), Times.Once);
        mockPersistenceService.Verify(p => p.PersistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        mockMetricsService.Verify(m => m.RecordEventFailedAsync(typeof(OrderCreatedEvent).Name, topic, It.IsAny<string>()), Times.Once);
        mockDeadLetterService.Verify(d => d.SendToDeadLetterQueueAsync(invalidEventJson, topic, It.IsAny<string>(), 0), Times.Once);
    }

    [Fact]
    public async Task ProcessEventAsync_PersistenceFailure_SendsToDeadLetterQueue()
    {
        // Arrange
        var mockValidationService = new Mock<IEventValidationService>();
        var mockPersistenceService = new Mock<IEventPersistenceService>();
        var mockMetricsService = new Mock<IMetricsService>();
        var mockDeadLetterService = new Mock<IDeadLetterQueueService>();
        var logger = Mock.Of<ILogger<EventProcessor>>();
        
        var processor = new EventProcessor(
            mockValidationService.Object,
            mockPersistenceService.Object,
            mockMetricsService.Object,
            mockDeadLetterService.Object,
            logger);

        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);
        var topic = "orders.order.created";

        mockValidationService
            .Setup(v => v.ValidateAsync(eventJson, typeof(OrderCreatedEvent)))
            .ReturnsAsync(true);

        mockPersistenceService
            .Setup(p => p.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, topic))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        await processor.ProcessEventAsync(eventJson, typeof(OrderCreatedEvent), topic);

        // Assert
        mockValidationService.Verify(v => v.ValidateAsync(eventJson, typeof(OrderCreatedEvent)), Times.Once);
        mockPersistenceService.Verify(p => p.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, topic), Times.Once);
        mockMetricsService.Verify(m => m.RecordEventFailedAsync(typeof(OrderCreatedEvent).Name, topic, "Database error"), Times.Once);
        mockDeadLetterService.Verify(d => d.SendToDeadLetterQueueAsync(eventJson, topic, "Database error", 0), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ProcessEventAsync_EmptyOrNullEventData_ThrowsException(string? eventData)
    {
        // Arrange
        var mockValidationService = new Mock<IEventValidationService>();
        var mockPersistenceService = new Mock<IEventPersistenceService>();
        var mockMetricsService = new Mock<IMetricsService>();
        var mockDeadLetterService = new Mock<IDeadLetterQueueService>();
        var logger = Mock.Of<ILogger<EventProcessor>>();
        
        var processor = new EventProcessor(
            mockValidationService.Object,
            mockPersistenceService.Object,
            mockMetricsService.Object,
            mockDeadLetterService.Object,
            logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            processor.ProcessEventAsync(eventData!, typeof(OrderCreatedEvent), "orders.order.created"));
    }

    [Fact]
    public async Task ProcessEventAsync_NullEventType_ThrowsException()
    {
        // Arrange
        var mockValidationService = new Mock<IEventValidationService>();
        var mockPersistenceService = new Mock<IEventPersistenceService>();
        var mockMetricsService = new Mock<IMetricsService>();
        var mockDeadLetterService = new Mock<IDeadLetterQueueService>();
        var logger = Mock.Of<ILogger<EventProcessor>>();
        
        var processor = new EventProcessor(
            mockValidationService.Object,
            mockPersistenceService.Object,
            mockMetricsService.Object,
            mockDeadLetterService.Object,
            logger);

        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            processor.ProcessEventAsync(eventJson, null!, "orders.order.created"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ProcessEventAsync_EmptyOrNullTopic_ThrowsException(string? topic)
    {
        // Arrange
        var mockValidationService = new Mock<IEventValidationService>();
        var mockPersistenceService = new Mock<IEventPersistenceService>();
        var mockMetricsService = new Mock<IMetricsService>();
        var mockDeadLetterService = new Mock<IDeadLetterQueueService>();
        var logger = Mock.Of<ILogger<EventProcessor>>();
        
        var processor = new EventProcessor(
            mockValidationService.Object,
            mockPersistenceService.Object,
            mockMetricsService.Object,
            mockDeadLetterService.Object,
            logger);

        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            processor.ProcessEventAsync(eventJson, typeof(OrderCreatedEvent), topic!));
    }

    [Fact]
    public async Task ProcessEventAsync_ValidationException_SendsToDeadLetterQueue()
    {
        // Arrange
        var mockValidationService = new Mock<IEventValidationService>();
        var mockPersistenceService = new Mock<IEventPersistenceService>();
        var mockMetricsService = new Mock<IMetricsService>();
        var mockDeadLetterService = new Mock<IDeadLetterQueueService>();
        var logger = Mock.Of<ILogger<EventProcessor>>();
        
        var processor = new EventProcessor(
            mockValidationService.Object,
            mockPersistenceService.Object,
            mockMetricsService.Object,
            mockDeadLetterService.Object,
            logger);

        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);
        var topic = "orders.order.created";

        mockValidationService
            .Setup(v => v.ValidateAsync(eventJson, typeof(OrderCreatedEvent)))
            .ThrowsAsync(new ValidationException("Validation error"));

        // Act
        await processor.ProcessEventAsync(eventJson, typeof(OrderCreatedEvent), topic);

        // Assert
        mockValidationService.Verify(v => v.ValidateAsync(eventJson, typeof(OrderCreatedEvent)), Times.Once);
        mockPersistenceService.Verify(p => p.PersistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        mockMetricsService.Verify(m => m.RecordEventFailedAsync(typeof(OrderCreatedEvent).Name, topic, "Validation error"), Times.Once);
        mockDeadLetterService.Verify(d => d.SendToDeadLetterQueueAsync(eventJson, topic, "Validation error", 0), Times.Once);
    }

    [Fact]
    public async Task ProcessEventAsync_MeasuresProcessingTime()
    {
        // Arrange
        var mockValidationService = new Mock<IEventValidationService>();
        var mockPersistenceService = new Mock<IEventPersistenceService>();
        var mockMetricsService = new Mock<IMetricsService>();
        var mockDeadLetterService = new Mock<IDeadLetterQueueService>();
        var logger = Mock.Of<ILogger<EventProcessor>>();
        
        var processor = new EventProcessor(
            mockValidationService.Object,
            mockPersistenceService.Object,
            mockMetricsService.Object,
            mockDeadLetterService.Object,
            logger);

        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);
        var topic = "orders.order.created";

        mockValidationService
            .Setup(v => v.ValidateAsync(eventJson, typeof(OrderCreatedEvent)))
            .ReturnsAsync(true);

        // Simulate some processing time
        mockPersistenceService
            .Setup(p => p.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, topic))
            .Returns(Task.Delay(100));

        // Act
        await processor.ProcessEventAsync(eventJson, typeof(OrderCreatedEvent), topic);

        // Assert
        mockMetricsService.Verify(
            m => m.RecordEventProcessedAsync(
                typeof(OrderCreatedEvent).Name,
                topic,
                It.Is<int>(time => time >= 100)), // Should be at least 100ms
            Times.Once);
    }

    [Fact]
    public async Task ProcessEventAsync_DifferentEventTypes_ProcessesCorrectly()
    {
        // Arrange
        var mockValidationService = new Mock<IEventValidationService>();
        var mockPersistenceService = new Mock<IEventPersistenceService>();
        var mockMetricsService = new Mock<IMetricsService>();
        var mockDeadLetterService = new Mock<IDeadLetterQueueService>();
        var logger = Mock.Of<ILogger<EventProcessor>>();
        
        var processor = new EventProcessor(
            mockValidationService.Object,
            mockPersistenceService.Object,
            mockMetricsService.Object,
            mockDeadLetterService.Object,
            logger);

        var paymentEvent = _autoFixture.Create<PaymentProcessedEvent>();
        var eventJson = JsonSerializer.Serialize(paymentEvent);
        var topic = "payments.payment.processed";

        mockValidationService
            .Setup(v => v.ValidateAsync(eventJson, typeof(PaymentProcessedEvent)))
            .ReturnsAsync(true);

        // Act
        await processor.ProcessEventAsync(eventJson, typeof(PaymentProcessedEvent), topic);

        // Assert
        mockValidationService.Verify(v => v.ValidateAsync(eventJson, typeof(PaymentProcessedEvent)), Times.Once);
        mockPersistenceService.Verify(p => p.PersistAsync(eventJson, typeof(PaymentProcessedEvent).Name, topic), Times.Once);
        mockMetricsService.Verify(m => m.RecordEventProcessedAsync(typeof(PaymentProcessedEvent).Name, topic, It.IsAny<int>()), Times.Once);
    }
}

// Helper exception class for testing
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
