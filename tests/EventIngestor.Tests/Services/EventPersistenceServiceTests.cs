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

public class EventPersistenceServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly IFixture _autoFixture;

    public EventPersistenceServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
    }

    [Fact]
    public async Task PersistAsync_ValidEvent_SavesSuccessfully()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventPersistenceService>>();
        var eventRepository = new EventRepository(dbContext);
        var service = new EventPersistenceService(eventRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act
        await service.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, "orders.order.created");

        // Assert
        var savedEvent = await dbContext.Events
            .FirstOrDefaultAsync(e => e.EventId == orderEvent.Id);
        
        savedEvent.Should().NotBeNull();
        savedEvent!.EventType.Should().Be(typeof(OrderCreatedEvent).Name);
        savedEvent.Topic.Should().Be("orders.order.created");
        savedEvent.EventData.Should().Be(eventJson);
    }

    [Fact]
    public async Task PersistAsync_DuplicateEvent_ThrowsException()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventPersistenceService>>();
        var eventRepository = new EventRepository(dbContext);
        var service = new EventPersistenceService(eventRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // First persist
        await service.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, "orders.order.created");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, "orders.order.created"));
    }

    [Fact]
    public async Task PersistAsync_ValidEvent_SetsTimestamp()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventPersistenceService>>();
        var eventRepository = new EventRepository(dbContext);
        var service = new EventPersistenceService(eventRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);
        var beforePersist = DateTime.UtcNow;

        // Act
        await service.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, "orders.order.created");

        // Assert
        var savedEvent = await dbContext.Events
            .FirstOrDefaultAsync(e => e.EventId == orderEvent.Id);
        
        savedEvent.Should().NotBeNull();
        savedEvent!.CreatedAt.Should().BeCloseTo(beforePersist, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task PersistAsync_ValidEvent_SetsVersion()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventPersistenceService>>();
        var eventRepository = new EventRepository(dbContext);
        var service = new EventPersistenceService(eventRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act
        await service.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, "orders.order.created");

        // Assert
        var savedEvent = await dbContext.Events
            .FirstOrDefaultAsync(e => e.EventId == orderEvent.Id);
        
        savedEvent.Should().NotBeNull();
        savedEvent!.Version.Should().Be(orderEvent.Version);
    }

    [Fact]
    public async Task PersistAsync_WithCorrelationId_SetsCorrelationId()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventPersistenceService>>();
        var eventRepository = new EventRepository(dbContext);
        var service = new EventPersistenceService(eventRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act
        await service.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, "orders.order.created");

        // Assert
        var savedEvent = await dbContext.Events
            .FirstOrDefaultAsync(e => e.EventId == orderEvent.Id);
        
        savedEvent.Should().NotBeNull();
        savedEvent!.CorrelationId.Should().Be(orderEvent.CorrelationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task PersistAsync_EmptyOrNullEventData_ThrowsException(string? eventData)
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventPersistenceService>>();
        var eventRepository = new EventRepository(dbContext);
        var service = new EventPersistenceService(eventRepository, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PersistAsync(eventData!, typeof(OrderCreatedEvent).Name, "orders.order.created"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task PersistAsync_EmptyOrNullEventType_ThrowsException(string? eventType)
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventPersistenceService>>();
        var eventRepository = new EventRepository(dbContext);
        var service = new EventPersistenceService(eventRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PersistAsync(eventJson, eventType!, "orders.order.created"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task PersistAsync_EmptyOrNullTopic_ThrowsException(string? topic)
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventPersistenceService>>();
        var eventRepository = new EventRepository(dbContext);
        var service = new EventPersistenceService(eventRepository, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, topic!));
    }

    [Fact]
    public async Task PersistAsync_DatabaseError_ThrowsException()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventPersistenceService>>();
        var mockEventRepository = new Mock<IEventRepository>();
        
        mockEventRepository
            .Setup(r => r.AddAsync(It.IsAny<Event>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        
        var service = new EventPersistenceService(mockEventRepository.Object, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, "orders.order.created"));
    }

    [Fact]
    public async Task PersistAsync_ValidEvent_CallsRepositoryAddAsync()
    {
        // Arrange
        var logger = Mock.Of<ILogger<EventPersistenceService>>();
        var mockEventRepository = new Mock<IEventRepository>();
        var service = new EventPersistenceService(mockEventRepository.Object, logger);
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act
        await service.PersistAsync(eventJson, typeof(OrderCreatedEvent).Name, "orders.order.created");

        // Assert
        mockEventRepository.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
    }
}
