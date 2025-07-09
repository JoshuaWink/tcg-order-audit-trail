using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.EventIngestor.Services;
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

public class DeadLetterQueueServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly IFixture _autoFixture;

    public DeadLetterQueueServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
    }

    [Fact]
    public async Task SendToDeadLetterQueueAsync_ValidMessage_SavesSuccessfully()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeadLetterQueueService>>();
        var deadLetterRepository = new DeadLetterQueueRepository(dbContext);
        var service = new DeadLetterQueueService(deadLetterRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);
        var errorMessage = "Validation failed";
        var topic = "orders.order.created";

        // Act
        await service.SendToDeadLetterQueueAsync(eventJson, topic, errorMessage);

        // Assert
        var dlqMessage = await dbContext.DeadLetterQueue
            .FirstOrDefaultAsync();
        
        dlqMessage.Should().NotBeNull();
        dlqMessage!.Topic.Should().Be(topic);
        dlqMessage.Message.Should().Be(eventJson);
        dlqMessage.ErrorMessage.Should().Be(errorMessage);
        dlqMessage.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task SendToDeadLetterQueueAsync_SetsTimestamp()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeadLetterQueueService>>();
        var deadLetterRepository = new DeadLetterQueueRepository(dbContext);
        var service = new DeadLetterQueueService(deadLetterRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);
        var beforeSend = DateTime.UtcNow;

        // Act
        await service.SendToDeadLetterQueueAsync(eventJson, "orders.order.created", "Error");

        // Assert
        var dlqMessage = await dbContext.DeadLetterQueue
            .FirstOrDefaultAsync();
        
        dlqMessage.Should().NotBeNull();
        dlqMessage!.CreatedAt.Should().BeCloseTo(beforeSend, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SendToDeadLetterQueueAsync_EmptyOrNullMessage_ThrowsException(string? message)
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeadLetterQueueService>>();
        var deadLetterRepository = new DeadLetterQueueRepository(dbContext);
        var service = new DeadLetterQueueService(deadLetterRepository, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendToDeadLetterQueueAsync(message!, "orders.order.created", "Error"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SendToDeadLetterQueueAsync_EmptyOrNullTopic_ThrowsException(string? topic)
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeadLetterQueueService>>();
        var deadLetterRepository = new DeadLetterQueueRepository(dbContext);
        var service = new DeadLetterQueueService(deadLetterRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendToDeadLetterQueueAsync(eventJson, topic!, "Error"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SendToDeadLetterQueueAsync_EmptyOrNullErrorMessage_ThrowsException(string? errorMessage)
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeadLetterQueueService>>();
        var deadLetterRepository = new DeadLetterQueueRepository(dbContext);
        var service = new DeadLetterQueueService(deadLetterRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendToDeadLetterQueueAsync(eventJson, "orders.order.created", errorMessage!));
    }

    [Fact]
    public async Task SendToDeadLetterQueueAsync_DatabaseError_ThrowsException()
    {
        // Arrange
        var logger = Mock.Of<ILogger<DeadLetterQueueService>>();
        var mockDeadLetterRepository = new Mock<IDeadLetterQueueRepository>();
        
        mockDeadLetterRepository
            .Setup(r => r.AddAsync(It.IsAny<DeadLetterQueueItem>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        
        var service = new DeadLetterQueueService(mockDeadLetterRepository.Object, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendToDeadLetterQueueAsync(eventJson, "orders.order.created", "Error"));
    }

    [Fact]
    public async Task SendToDeadLetterQueueAsync_CallsRepositoryAddAsync()
    {
        // Arrange
        var logger = Mock.Of<ILogger<DeadLetterQueueService>>();
        var mockDeadLetterRepository = new Mock<IDeadLetterQueueRepository>();
        var service = new DeadLetterQueueService(mockDeadLetterRepository.Object, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act
        await service.SendToDeadLetterQueueAsync(eventJson, "orders.order.created", "Error");

        // Assert
        mockDeadLetterRepository.Verify(r => r.AddAsync(It.IsAny<DeadLetterQueueItem>()), Times.Once);
    }

    [Fact]
    public async Task SendToDeadLetterQueueAsync_LogsError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DeadLetterQueueService>>();
        var mockDeadLetterRepository = new Mock<IDeadLetterQueueRepository>();
        var service = new DeadLetterQueueService(mockDeadLetterRepository.Object, mockLogger.Object);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);
        var errorMessage = "Validation failed";
        var topic = "orders.order.created";

        // Act
        await service.SendToDeadLetterQueueAsync(eventJson, topic, errorMessage);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sending message to dead letter queue")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendToDeadLetterQueueAsync_WithRetryCount_SetsRetryCount()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeadLetterQueueService>>();
        var deadLetterRepository = new DeadLetterQueueRepository(dbContext);
        var service = new DeadLetterQueueService(deadLetterRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);
        var retryCount = 3;

        // Act
        await service.SendToDeadLetterQueueAsync(eventJson, "orders.order.created", "Error", retryCount);

        // Assert
        var dlqMessage = await dbContext.DeadLetterQueue
            .FirstOrDefaultAsync();
        
        dlqMessage.Should().NotBeNull();
        dlqMessage!.RetryCount.Should().Be(retryCount);
    }

    [Fact]
    public async Task SendToDeadLetterQueueAsync_WithNegativeRetryCount_ThrowsException()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeadLetterQueueService>>();
        var deadLetterRepository = new DeadLetterQueueRepository(dbContext);
        var service = new DeadLetterQueueService(deadLetterRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendToDeadLetterQueueAsync(eventJson, "orders.order.created", "Error", -1));
    }
}
