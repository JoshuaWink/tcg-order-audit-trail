using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.EventIngestor.Services;
using OrderAuditTrail.Shared.Configuration;
using OrderAuditTrail.Shared.Data;
using OrderAuditTrail.Shared.Events.Orders;
using OrderAuditTrail.Shared.Models;
using FluentAssertions;
using AutoFixture;
using System.Text.Json;
using Confluent.Kafka;
using Testcontainers.Kafka;

namespace OrderAuditTrail.EventIngestor.Tests.Integration;

public class EventIngestorIntegrationTests : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private readonly IFixture _autoFixture;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _testTopic = "test-orders-created";

    public EventIngestorIntegrationTests(TestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
        _serviceProvider = _fixture.TestConfiguration.ServiceProvider;
    }

    public async Task InitializeAsync()
    {
        // Create test topic
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _fixture.TestConfiguration.KafkaBootstrapServers
        }).Build();

        await adminClient.CreateTopicsAsync(new[]
        {
            new TopicSpecification
            {
                Name = _testTopic,
                NumPartitions = 1,
                ReplicationFactor = 1
            }
        });
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProcessEvent_ValidOrderCreatedEvent_SavesToDatabase()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var eventProcessor = scope.ServiceProvider.GetRequiredService<EventProcessor>();
        
        var orderEvent = new OrderCreatedEvent
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Source = "integration-test",
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 99.99m,
            Currency = "USD",
            Status = "Created",
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    Price = 99.99m,
                    Currency = "USD"
                }
            }
        };

        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act
        await eventProcessor.ProcessEventAsync(eventJson, typeof(OrderCreatedEvent), _testTopic);

        // Assert
        var savedEvent = await dbContext.Events
            .FirstOrDefaultAsync(e => e.EventId == orderEvent.Id);

        savedEvent.Should().NotBeNull();
        savedEvent!.EventType.Should().Be(typeof(OrderCreatedEvent).Name);
        savedEvent.Topic.Should().Be(_testTopic);
        savedEvent.EventData.Should().Be(eventJson);
        savedEvent.CorrelationId.Should().Be(orderEvent.CorrelationId);
        savedEvent.Version.Should().Be(orderEvent.Version);
    }

    [Fact]
    public async Task ProcessEvent_InvalidEvent_SavesToDeadLetterQueue()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var eventProcessor = scope.ServiceProvider.GetRequiredService<EventProcessor>();
        
        var invalidEventJson = "{ \"invalid\": \"json\" }"; // Missing required fields

        // Act
        await eventProcessor.ProcessEventAsync(invalidEventJson, typeof(OrderCreatedEvent), _testTopic);

        // Assert
        var dlqItem = await dbContext.DeadLetterQueue
            .FirstOrDefaultAsync(d => d.Topic == _testTopic);

        dlqItem.Should().NotBeNull();
        dlqItem!.Message.Should().Be(invalidEventJson);
        dlqItem.Topic.Should().Be(_testTopic);
        dlqItem.ErrorMessage.Should().Contain("validation");
    }

    [Fact]
    public async Task ProcessEvent_ValidEvent_RecordsMetrics()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var eventProcessor = scope.ServiceProvider.GetRequiredService<EventProcessor>();
        
        var orderEvent = _autoFixture.Create<OrderCreatedEvent>();
        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act
        await eventProcessor.ProcessEventAsync(eventJson, typeof(OrderCreatedEvent), _testTopic);

        // Assert
        var metric = await dbContext.Metrics
            .FirstOrDefaultAsync(m => m.EventType == typeof(OrderCreatedEvent).Name && m.Topic == _testTopic);

        metric.Should().NotBeNull();
        metric!.EventType.Should().Be(typeof(OrderCreatedEvent).Name);
        metric.Topic.Should().Be(_testTopic);
        metric.Status.Should().Be("Success");
        metric.ProcessingTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessEvent_MultipleEvents_ProcessesAllCorrectly()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var eventProcessor = scope.ServiceProvider.GetRequiredService<EventProcessor>();
        
        var events = _autoFixture.CreateMany<OrderCreatedEvent>(5).ToList();
        var eventJsons = events.Select(e => JsonSerializer.Serialize(e)).ToList();

        // Act
        foreach (var eventJson in eventJsons)
        {
            await eventProcessor.ProcessEventAsync(eventJson, typeof(OrderCreatedEvent), _testTopic);
        }

        // Assert
        var savedEvents = await dbContext.Events
            .Where(e => e.EventType == typeof(OrderCreatedEvent).Name && e.Topic == _testTopic)
            .ToListAsync();

        savedEvents.Should().HaveCount(5);
        
        foreach (var originalEvent in events)
        {
            var savedEvent = savedEvents.FirstOrDefault(e => e.EventId == originalEvent.Id);
            savedEvent.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task KafkaConsumer_Configuration_UsesCorrectSettings()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var kafkaSettings = scope.ServiceProvider.GetRequiredService<IOptions<KafkaSettings>>().Value;
        var topicSettings = scope.ServiceProvider.GetRequiredService<IOptions<TopicSettings>>().Value;

        // Assert
        kafkaSettings.Should().NotBeNull();
        kafkaSettings.BootstrapServers.Should().Be(_fixture.TestConfiguration.KafkaBootstrapServers);
        kafkaSettings.ConsumerGroupId.Should().Be("audit-trail-ingestor-test");
        kafkaSettings.AutoOffsetReset.Should().Be("earliest");
        kafkaSettings.EnableAutoCommit.Should().Be("false");
        
        topicSettings.Should().NotBeNull();
        topicSettings.OrdersCreated.Should().Be("orders.order.created");
        topicSettings.OrdersUpdated.Should().Be("orders.order.updated");
        topicSettings.OrdersCancelled.Should().Be("orders.order.cancelled");
    }

    [Fact]
    public async Task Database_Configuration_UsesCorrectSettings()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var databaseSettings = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;

        // Assert
        databaseSettings.Should().NotBeNull();
        databaseSettings.Host.Should().NotBeNullOrEmpty();
        databaseSettings.Port.Should().BeGreaterThan(0);
        databaseSettings.Database.Should().Be("audit_trail_test");
        databaseSettings.Username.Should().Be("test_user");
        databaseSettings.MinPoolSize.Should().Be(5);
        databaseSettings.MaxPoolSize.Should().Be(100);
        databaseSettings.CommandTimeoutSeconds.Should().Be(30);

        // Verify database connection works
        await dbContext.Database.OpenConnectionAsync();
        dbContext.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Open);
    }

    [Fact]
    public async Task EventProcessor_DependencyInjection_ResolvesAllServices()
    {
        // Arrange & Act
        using var scope = _serviceProvider.CreateScope();
        
        var eventProcessor = scope.ServiceProvider.GetRequiredService<EventProcessor>();
        var validationService = scope.ServiceProvider.GetRequiredService<IEventValidationService>();
        var persistenceService = scope.ServiceProvider.GetRequiredService<IEventPersistenceService>();
        var metricsService = scope.ServiceProvider.GetRequiredService<IMetricsService>();
        var deadLetterService = scope.ServiceProvider.GetRequiredService<IDeadLetterQueueService>();

        // Assert
        eventProcessor.Should().NotBeNull();
        validationService.Should().NotBeNull();
        persistenceService.Should().NotBeNull();
        metricsService.Should().NotBeNull();
        deadLetterService.Should().NotBeNull();
    }

    [Fact]
    public async Task Configuration_AllSettingsFromEnvExample_AreLoaded()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act & Assert - Database settings
        configuration["DatabaseSettings:Host"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:Port"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:Database"].Should().Be("audit_trail_test");
        configuration["DatabaseSettings:Username"].Should().Be("test_user");
        configuration["DatabaseSettings:SslMode"].Should().Be("prefer");
        configuration["DatabaseSettings:MinPoolSize"].Should().Be("5");
        configuration["DatabaseSettings:MaxPoolSize"].Should().Be("100");
        configuration["DatabaseSettings:CommandTimeoutSeconds"].Should().Be("30");
        configuration["DatabaseSettings:ConnectionTimeoutSeconds"].Should().Be("15");

        // Kafka settings
        configuration["KafkaSettings:BootstrapServers"].Should().NotBeNullOrEmpty();
        configuration["KafkaSettings:SecurityProtocol"].Should().Be("PLAINTEXT");
        configuration["KafkaSettings:ConsumerGroupId"].Should().Be("audit-trail-ingestor-test");
        configuration["KafkaSettings:AutoOffsetReset"].Should().Be("earliest");
        configuration["KafkaSettings:EnableAutoCommit"].Should().Be("false");
        configuration["KafkaSettings:MaxPollIntervalMs"].Should().Be("300000");
        configuration["KafkaSettings:ProducerAcks"].Should().Be("all");
        configuration["KafkaSettings:ProducerEnableIdempotence"].Should().Be("true");
        configuration["KafkaSettings:ProducerRetries"].Should().Be("5");

        // Topic settings
        configuration["TopicSettings:OrdersCreated"].Should().Be("orders.order.created");
        configuration["TopicSettings:OrdersUpdated"].Should().Be("orders.order.updated");
        configuration["TopicSettings:OrdersCancelled"].Should().Be("orders.order.cancelled");
        configuration["TopicSettings:PaymentsProcessed"].Should().Be("payments.payment.processed");
        configuration["TopicSettings:PaymentsFailed"].Should().Be("payments.payment.failed");
        configuration["TopicSettings:InventoryUpdated"].Should().Be("inventory.item.updated");
        configuration["TopicSettings:ShippingCreated"].Should().Be("shipping.shipment.created");
        configuration["TopicSettings:ShippingDelivered"].Should().Be("shipping.shipment.delivered");

        // Monitoring settings
        configuration["MonitoringSettings:MetricsEnabled"].Should().Be("true");
        configuration["MonitoringSettings:MetricsPort"].Should().Be("9090");
        configuration["MonitoringSettings:HealthCheckPath"].Should().Be("/health");
        configuration["MonitoringSettings:HealthCheckTimeoutSeconds"].Should().Be("30");

        // Development settings
        configuration["DevelopmentSettings:DevelopmentMode"].Should().Be("true");
        configuration["DevelopmentSettings:TestDatabaseName"].Should().Be("audit_trail_test");
        configuration["DevelopmentSettings:IntegrationTestMode"].Should().Be("true");
    }

    [Fact]
    public async Task EventProcessor_LargeEvent_ProcessesSuccessfully()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var eventProcessor = scope.ServiceProvider.GetRequiredService<EventProcessor>();
        
        var orderEvent = new OrderCreatedEvent
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Source = "integration-test",
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 1000.00m,
            Currency = "USD",
            Status = "Created",
            Items = Enumerable.Range(1, 100).Select(i => new OrderItem
            {
                ProductId = Guid.NewGuid(),
                Quantity = i,
                Price = 10.00m,
                Currency = "USD"
            }).ToList()
        };

        var eventJson = JsonSerializer.Serialize(orderEvent);

        // Act
        await eventProcessor.ProcessEventAsync(eventJson, typeof(OrderCreatedEvent), _testTopic);

        // Assert
        var savedEvent = await dbContext.Events
            .FirstOrDefaultAsync(e => e.EventId == orderEvent.Id);

        savedEvent.Should().NotBeNull();
        savedEvent!.EventData.Should().Be(eventJson);
        savedEvent.EventData.Length.Should().BeGreaterThan(5000); // Large event
    }
}
