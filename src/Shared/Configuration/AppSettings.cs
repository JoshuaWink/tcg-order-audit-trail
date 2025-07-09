namespace OrderAuditTrail.Shared.Configuration;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppSettings
{
    public const string SectionName = "AppSettings";

    /// <summary>
    /// Database connection settings
    /// </summary>
    public DatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// Kafka settings
    /// </summary>
    public KafkaSettings Kafka { get; set; } = new();

    /// <summary>
    /// Redis settings
    /// </summary>
    public RedisSettings Redis { get; set; } = new();

    /// <summary>
    /// API settings
    /// </summary>
    public ApiSettings Api { get; set; } = new();

    /// <summary>
    /// Event processing settings
    /// </summary>
    public EventProcessingSettings EventProcessing { get; set; } = new();

    /// <summary>
    /// Monitoring and observability settings
    /// </summary>
    public MonitoringSettings Monitoring { get; set; } = new();

    /// <summary>
    /// Security settings
    /// </summary>
    public SecuritySettings Security { get; set; } = new();
}

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// PostgreSQL connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for database operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Enable detailed logging of database operations
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Enable automatic database migrations
    /// </summary>
    public bool EnableAutoMigration { get; set; } = false;
}

/// <summary>
/// Kafka configuration settings
/// </summary>
public class KafkaSettings
{
    /// <summary>
    /// Kafka bootstrap servers
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Security protocol (PLAINTEXT, SSL, SASL_PLAINTEXT, SASL_SSL)
    /// </summary>
    public string SecurityProtocol { get; set; } = "PLAINTEXT";

    /// <summary>
    /// SASL mechanism (PLAIN, SCRAM-SHA-256, SCRAM-SHA-512)
    /// </summary>
    public string SaslMechanism { get; set; } = "PLAIN";

    /// <summary>
    /// SASL username
    /// </summary>
    public string SaslUsername { get; set; } = string.Empty;

    /// <summary>
    /// SASL password
    /// </summary>
    public string SaslPassword { get; set; } = string.Empty;

    /// <summary>
    /// Consumer group ID for event processing
    /// </summary>
    public string ConsumerGroupId { get; set; } = "audit-trail-consumer";

    /// <summary>
    /// Topic configuration for different event types
    /// </summary>
    public Dictionary<string, TopicSettings> Topics { get; set; } = new()
    {
        { "order-events", new TopicSettings { Name = "order-events", Partitions = 3, ReplicationFactor = 1 } },
        { "payment-events", new TopicSettings { Name = "payment-events", Partitions = 3, ReplicationFactor = 1 } },
        { "inventory-events", new TopicSettings { Name = "inventory-events", Partitions = 3, ReplicationFactor = 1 } },
        { "shipping-events", new TopicSettings { Name = "shipping-events", Partitions = 3, ReplicationFactor = 1 } }
    };

    /// <summary>
    /// Consumer configuration
    /// </summary>
    public ConsumerSettings Consumer { get; set; } = new();

    /// <summary>
    /// Producer configuration
    /// </summary>
    public ProducerSettings Producer { get; set; } = new();
}

/// <summary>
/// Kafka topic settings
/// </summary>
public class TopicSettings
{
    /// <summary>
    /// Topic name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of partitions
    /// </summary>
    public int Partitions { get; set; } = 1;

    /// <summary>
    /// Replication factor
    /// </summary>
    public short ReplicationFactor { get; set; } = 1;

    /// <summary>
    /// Additional topic configuration
    /// </summary>
    public Dictionary<string, string> Config { get; set; } = new();
}

/// <summary>
/// Kafka consumer settings
/// </summary>
public class ConsumerSettings
{
    /// <summary>
    /// Auto offset reset policy (earliest, latest, none)
    /// </summary>
    public string AutoOffsetReset { get; set; } = "earliest";

    /// <summary>
    /// Enable auto commit
    /// </summary>
    public bool EnableAutoCommit { get; set; } = false;

    /// <summary>
    /// Maximum number of messages to fetch in a single poll
    /// </summary>
    public int MaxPollRecords { get; set; } = 500;

    /// <summary>
    /// Session timeout in milliseconds
    /// </summary>
    public int SessionTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Heartbeat interval in milliseconds
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 3000;

    /// <summary>
    /// Maximum time to wait for new messages
    /// </summary>
    public int PollTimeoutMs { get; set; } = 1000;
}

/// <summary>
/// Kafka producer settings
/// </summary>
public class ProducerSettings
{
    /// <summary>
    /// Acknowledgment mode (all, 1, 0)
    /// </summary>
    public string Acks { get; set; } = "all";

    /// <summary>
    /// Number of retries
    /// </summary>
    public int Retries { get; set; } = 3;

    /// <summary>
    /// Batch size for batching messages
    /// </summary>
    public int BatchSize { get; set; } = 16384;

    /// <summary>
    /// Linger time in milliseconds
    /// </summary>
    public int LingerMs { get; set; } = 5;

    /// <summary>
    /// Buffer memory size
    /// </summary>
    public int BufferMemory { get; set; } = 33554432;

    /// <summary>
    /// Compression type (none, gzip, snappy, lz4, zstd)
    /// </summary>
    public string CompressionType { get; set; } = "gzip";
}

/// <summary>
/// Redis configuration settings
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Redis connection string
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Database number
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// Key prefix for caching
    /// </summary>
    public string KeyPrefix { get; set; } = "audit:";

    /// <summary>
    /// Default cache expiration in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// API configuration settings
/// </summary>
public class ApiSettings
{
    /// <summary>
    /// API base URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:7001";

    /// <summary>
    /// API version
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// Maximum page size for paginated responses
    /// </summary>
    public int MaxPageSize { get; set; } = 1000;

    /// <summary>
    /// Default page size for paginated responses
    /// </summary>
    public int DefaultPageSize { get; set; } = 100;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable API documentation (Swagger)
    /// </summary>
    public bool EnableSwagger { get; set; } = true;

    /// <summary>
    /// Enable detailed error responses
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// CORS settings
    /// </summary>
    public CorsSettings Cors { get; set; } = new();
}

/// <summary>
/// CORS configuration settings
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// Allowed origins
    /// </summary>
    public string[] AllowedOrigins { get; set; } = { "*" };

    /// <summary>
    /// Allowed methods
    /// </summary>
    public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };

    /// <summary>
    /// Allowed headers
    /// </summary>
    public string[] AllowedHeaders { get; set; } = { "*" };

    /// <summary>
    /// Allow credentials
    /// </summary>
    public bool AllowCredentials { get; set; } = false;
}

/// <summary>
/// Event processing configuration settings
/// </summary>
public class EventProcessingSettings
{
    /// <summary>
    /// Number of worker threads for processing events
    /// </summary>
    public int WorkerThreads { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Batch size for processing events
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum number of retry attempts for failed events
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Dead letter queue settings
    /// </summary>
    public DeadLetterQueueSettings DeadLetterQueue { get; set; } = new();
}

/// <summary>
/// Dead letter queue settings
/// </summary>
public class DeadLetterQueueSettings
{
    /// <summary>
    /// Enable dead letter queue
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum retention period for dead letter messages in days
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Enable automatic reprocessing of dead letter messages
    /// </summary>
    public bool EnableAutoReprocessing { get; set; } = false;

    /// <summary>
    /// Reprocessing interval in minutes
    /// </summary>
    public int ReprocessingIntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Monitoring and observability settings
/// </summary>
public class MonitoringSettings
{
    /// <summary>
    /// Enable health checks
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check endpoint path
    /// </summary>
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Enable metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Metrics endpoint path
    /// </summary>
    public string MetricsPath { get; set; } = "/metrics";

    /// <summary>
    /// Enable distributed tracing
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Tracing service name
    /// </summary>
    public string ServiceName { get; set; } = "audit-trail";

    /// <summary>
    /// Log level (Debug, Information, Warning, Error, Critical)
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}

/// <summary>
/// Security configuration settings
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Enable authentication
    /// </summary>
    public bool EnableAuthentication { get; set; } = true;

    /// <summary>
    /// Enable authorization
    /// </summary>
    public bool EnableAuthorization { get; set; } = true;

    /// <summary>
    /// JWT settings
    /// </summary>
    public JwtSettings Jwt { get; set; } = new();

    /// <summary>
    /// API key settings
    /// </summary>
    public ApiKeySettings ApiKey { get; set; } = new();
}

/// <summary>
/// JWT configuration settings
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// JWT secret key
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT issuer
    /// </summary>
    public string Issuer { get; set; } = "audit-trail";

    /// <summary>
    /// JWT audience
    /// </summary>
    public string Audience { get; set; } = "audit-trail-api";

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration time in days
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

/// <summary>
/// API key configuration settings
/// </summary>
public class ApiKeySettings
{
    /// <summary>
    /// Enable API key authentication
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// API key header name
    /// </summary>
    public string HeaderName { get; set; } = "X-API-Key";

    /// <summary>
    /// Valid API keys
    /// </summary>
    public Dictionary<string, string> Keys { get; set; } = new();
}
