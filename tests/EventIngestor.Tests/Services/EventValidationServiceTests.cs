using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderAuditTrail.EventIngestor.Services;
using OrderAuditTrail.Shared.Configuration;
using OrderAuditTrail.Shared.Data;
using OrderAuditTrail.Shared.Events;
using OrderAuditTrail.Shared.Events.Orders;
using OrderAuditTrail.Shared.Models;
using FluentAssertions;
using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using System.Text.Json;

namespace OrderAuditTrail.EventIngestor.Tests.Services;

public class EventValidationServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly IFixture _autoFixture;

    public EventValidationServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
    }

    [Fact]
    public async Task ValidateAsync_ValidEvent_ReturnsTrue()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventValidationService>>();
        var service = new EventValidationService(logger);
        
        var validEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(validEvent);

        // Act
        var result = await service.ValidateAsync(eventJson, typeof(OrderCreatedEvent));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_InvalidJson_ReturnsFalse()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventValidationService>>();
        var service = new EventValidationService(logger);
        
        var invalidJson = "{ invalid json }";

        // Act
        var result = await service.ValidateAsync(invalidJson, typeof(OrderCreatedEvent));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_MissingRequiredFields_ReturnsFalse()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventValidationService>>();
        var service = new EventValidationService(logger);
        
        var incompleteEvent = new { Id = Guid.NewGuid() }; // Missing required fields
        var eventJson = JsonSerializer.Serialize(incompleteEvent);

        // Act
        var result = await service.ValidateAsync(eventJson, typeof(OrderCreatedEvent));

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ValidateAsync_EmptyOrNullJson_ReturnsFalse(string? json)
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventValidationService>>();
        var service = new EventValidationService(logger);

        // Act
        var result = await service.ValidateAsync(json!, typeof(OrderCreatedEvent));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_NullEventType_ReturnsFalse()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventValidationService>>();
        var service = new EventValidationService(logger);
        
        var validEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(validEvent);

        // Act
        var result = await service.ValidateAsync(eventJson, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_EventWithAllRequiredFields_ReturnsTrue()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventValidationService>>();
        var service = new EventValidationService(logger);
        
        var validEvent = new OrderCreatedEvent
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Source = "test-source",
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 100.50m,
            Currency = "USD",
            Status = "Created",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2,
                    Price = 50.25m,
                    Currency = "USD"
                }
            }
        };
        
        var eventJson = JsonSerializer.Serialize(validEvent);

        // Act
        var result = await service.ValidateAsync(eventJson, typeof(OrderCreatedEvent));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_EventWithInvalidGuid_ReturnsFalse()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventValidationService>>();
        var service = new EventValidationService(logger);
        
        var invalidEvent = new
        {
            Id = "invalid-guid",
            Version = 1,
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Source = "test-source",
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 100.50m,
            Currency = "USD",
            Status = "Created",
            Items = new List<OrderItem>()
        };
        
        var eventJson = JsonSerializer.Serialize(invalidEvent);

        // Act
        var result = await service.ValidateAsync(eventJson, typeof(OrderCreatedEvent));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_EventWithNegativeAmount_ReturnsFalse()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventValidationService>>();
        var service = new EventValidationService(logger);
        
        var invalidEvent = new
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Source = "test-source",
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = -100.50m, // Negative amount
            Currency = "USD",
            Status = "Created",
            Items = new List<OrderItem>()
        };
        
        var eventJson = JsonSerializer.Serialize(invalidEvent);

        // Act
        var result = await service.ValidateAsync(eventJson, typeof(OrderCreatedEvent));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_EventWithFutureTimestamp_ReturnsFalse()
    {
        // Arrange
        using var scope = _fixture.TestConfiguration.ServiceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EventValidationService>>();
        var service = new EventValidationService(logger);
        
        var invalidEvent = new
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Timestamp = DateTime.UtcNow.AddHours(1), // Future timestamp
            CorrelationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Source = "test-source",
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 100.50m,
            Currency = "USD",
            Status = "Created",
            Items = new List<OrderItem>()
        };
        
        var eventJson = JsonSerializer.Serialize(invalidEvent);

        // Act
        var result = await service.ValidateAsync(eventJson, typeof(OrderCreatedEvent));

        // Assert
        result.Should().BeFalse();
    }
}
