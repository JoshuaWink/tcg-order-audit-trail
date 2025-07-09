using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Data;
using OrderAuditTrail.Shared.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.Kafka;

namespace OrderAuditTrail.EventIngestor.Tests;

public class TestConfiguration : IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly KafkaContainer _kafkaContainer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    
    public TestConfiguration()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("audit_trail_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.5.0")
            .Build();

        _configuration = BuildConfiguration();
        _serviceProvider = BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _kafkaContainer.StartAsync();
        
        // Apply database migrations
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    private IConfiguration BuildConfiguration()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Database Configuration - using container values
                ["DatabaseSettings:Host"] = _postgresContainer.Hostname,
                ["DatabaseSettings:Port"] = _postgresContainer.GetMappedPublicPort(5432).ToString(),
                ["DatabaseSettings:Database"] = "audit_trail_test",
                ["DatabaseSettings:Username"] = "test_user",
                ["DatabaseSettings:Password"] = "test_password",
                ["DatabaseSettings:SslMode"] = "prefer",
                ["DatabaseSettings:MinPoolSize"] = "5",
                ["DatabaseSettings:MaxPoolSize"] = "100",
                ["DatabaseSettings:CommandTimeoutSeconds"] = "30",
                ["DatabaseSettings:ConnectionTimeoutSeconds"] = "15",
                
                // Kafka Configuration - using container values
                ["KafkaSettings:BootstrapServers"] = _kafkaContainer.GetBootstrapAddress(),
                ["KafkaSettings:SecurityProtocol"] = "PLAINTEXT",
                ["KafkaSettings:SaslMechanism"] = "PLAIN",
                ["KafkaSettings:SaslUsername"] = "",
                ["KafkaSettings:SaslPassword"] = "",
                ["KafkaSettings:ConsumerGroupId"] = "audit-trail-ingestor-test",
                ["KafkaSettings:AutoOffsetReset"] = "earliest",
                ["KafkaSettings:EnableAutoCommit"] = "false",
                ["KafkaSettings:MaxPollIntervalMs"] = "300000",
                ["KafkaSettings:ProducerClientId"] = "audit-trail-producer-test",
                ["KafkaSettings:ProducerAcks"] = "all",
                ["KafkaSettings:ProducerEnableIdempotence"] = "true",
                ["KafkaSettings:ProducerRetries"] = "5",
                
                // Topic Configuration - matching .env.example
                ["TopicSettings:OrdersCreated"] = "orders.order.created",
                ["TopicSettings:OrdersUpdated"] = "orders.order.updated",
                ["TopicSettings:OrdersCancelled"] = "orders.order.cancelled",
                ["TopicSettings:PaymentsProcessed"] = "payments.payment.processed",
                ["TopicSettings:PaymentsFailed"] = "payments.payment.failed",
                ["TopicSettings:InventoryUpdated"] = "inventory.item.updated",
                ["TopicSettings:ShippingCreated"] = "shipping.shipment.created",
                ["TopicSettings:ShippingDelivered"] = "shipping.shipment.delivered",
                
                // Logging Configuration - matching .env.example
                ["LoggingSettings:LogLevel"] = "Information",
                ["LoggingSettings:ConsoleEnabled"] = "true",
                ["LoggingSettings:FileEnabled"] = "false", // Disable file logging for tests
                ["LoggingSettings:FilePath"] = "logs/audit-trail-test.log",
                ["LoggingSettings:FileMaxSizeMB"] = "100",
                ["LoggingSettings:FileMaxFiles"] = "10",
                
                // Monitoring Configuration - matching .env.example
                ["MonitoringSettings:MetricsEnabled"] = "true",
                ["MonitoringSettings:MetricsPort"] = "9090",
                ["MonitoringSettings:HealthCheckPath"] = "/health",
                ["MonitoringSettings:HealthCheckTimeoutSeconds"] = "30",
                
                // Encryption Configuration - matching .env.example
                ["EncryptionSettings:Key"] = "test-32-character-encryption-key-here-change-this",
                ["EncryptionSettings:IV"] = "test-16-char-iv-here",
                
                // Performance Configuration - matching .env.example
                ["PerformanceSettings:ConnectionPoolMinSize"] = "5",
                ["PerformanceSettings:ConnectionPoolMaxSize"] = "100",
                ["PerformanceSettings:CommandTimeoutSeconds"] = "30",
                ["PerformanceSettings:ConnectionTimeoutSeconds"] = "15",
                
                // Caching Configuration - matching .env.example
                ["CachingSettings:RedisHost"] = "localhost",
                ["CachingSettings:RedisPort"] = "6379",
                ["CachingSettings:RedisPassword"] = "",
                ["CachingSettings:RedisDatabase"] = "0",
                ["CachingSettings:RedisSslEnabled"] = "false",
                ["CachingSettings:CacheTtlSeconds"] = "300",
                ["CachingSettings:CacheEnabled"] = "false", // Disable caching for tests
                
                // Background Job Configuration - matching .env.example
                ["BackgroundJobSettings:Enabled"] = "false", // Disable background jobs for tests
                ["BackgroundJobSettings:IntervalSeconds"] = "60",
                ["BackgroundJobSettings:CleanupOldEventsEnabled"] = "false",
                ["BackgroundJobSettings:CleanupOldEventsRetentionDays"] = "365",
                
                // Development/Testing Configuration - matching .env.example
                ["DevelopmentSettings:DevelopmentMode"] = "true",
                ["DevelopmentSettings:SeedTestData"] = "false",
                ["DevelopmentSettings:TestDatabaseName"] = "audit_trail_test",
                ["DevelopmentSettings:IntegrationTestMode"] = "true"
            });

        return configBuilder.Build();
    }

    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add configuration
        services.AddSingleton(_configuration);
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });
        
        // Add database context
        services.AddDbContext<AuditDbContext>(options =>
        {
            options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
        });
        
        // Add configuration settings
        services.Configure<DatabaseSettings>(_configuration.GetSection("DatabaseSettings"));
        services.Configure<KafkaSettings>(_configuration.GetSection("KafkaSettings"));
        services.Configure<TopicSettings>(_configuration.GetSection("TopicSettings"));
        services.Configure<LoggingSettings>(_configuration.GetSection("LoggingSettings"));
        services.Configure<MonitoringSettings>(_configuration.GetSection("MonitoringSettings"));
        services.Configure<EncryptionSettings>(_configuration.GetSection("EncryptionSettings"));
        services.Configure<PerformanceSettings>(_configuration.GetSection("PerformanceSettings"));
        services.Configure<CachingSettings>(_configuration.GetSection("CachingSettings"));
        services.Configure<BackgroundJobSettings>(_configuration.GetSection("BackgroundJobSettings"));
        services.Configure<DevelopmentSettings>(_configuration.GetSection("DevelopmentSettings"));
        
        return services.BuildServiceProvider();
    }

    public IServiceProvider ServiceProvider => _serviceProvider;
    public IConfiguration Configuration => _configuration;
    public string PostgresConnectionString => _postgresContainer.GetConnectionString();
    public string KafkaBootstrapServers => _kafkaContainer.GetBootstrapAddress();

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _serviceProvider?.Dispose();
        }
        
        await _postgresContainer.DisposeAsync();
        await _kafkaContainer.DisposeAsync();
    }
}

public class TestFixture : IAsyncLifetime
{
    public TestConfiguration TestConfiguration { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        TestConfiguration = new TestConfiguration();
        await TestConfiguration.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (TestConfiguration != null)
        {
            await TestConfiguration.DisposeAsync();
        }
    }
}
