using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderAuditTrail.Shared.Models;

/// <summary>
/// Entity representing a persisted event in the database
/// </summary>
[Table("events")]
public class EventEntity
{
    /// <summary>
    /// Primary key (auto-generated)
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    [Column("event_id")]
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Type of the event
    /// </summary>
    [Column("event_type")]
    [Required]
    [MaxLength(255)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the aggregate that generated this event
    /// </summary>
    [Column("aggregate_id")]
    [Required]
    [MaxLength(255)]
    public string AggregateId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the aggregate
    /// </summary>
    [Column("aggregate_type")]
    [Required]
    [MaxLength(255)]
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    [Column("timestamp")]
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Version of the event within the aggregate stream
    /// </summary>
    [Column("version")]
    [Required]
    public int Version { get; set; }

    /// <summary>
    /// Event payload as JSON
    /// </summary>
    [Column("payload", TypeName = "jsonb")]
    [Required]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Event metadata as JSON
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    [Required]
    public string Metadata { get; set; } = string.Empty;

    /// <summary>
    /// Source system that generated the event
    /// </summary>
    [Column("source")]
    [MaxLength(255)]
    public string? Source { get; set; }

    /// <summary>
    /// Kafka topic the event came from
    /// </summary>
    [Column("topic")]
    [MaxLength(255)]
    public string? Topic { get; set; }

    /// <summary>
    /// Kafka partition the event came from
    /// </summary>
    [Column("partition")]
    public int? Partition { get; set; }

    /// <summary>
    /// Kafka offset of the event
    /// </summary>
    [Column("offset")]
    public long? Offset { get; set; }

    /// <summary>
    /// Full event data as JSON
    /// </summary>
    [Column("event_data", TypeName = "jsonb")]
    public string? EventData { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    [Column("correlation_id")]
    [MaxLength(255)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Causation ID for event causation tracking
    /// </summary>
    [Column("causation_id")]
    [MaxLength(255)]
    public string? CausationId { get; set; }

    /// <summary>
    /// User ID associated with the event
    /// </summary>
    [Column("user_id")]
    [MaxLength(255)]
    public string? UserId { get; set; }

    /// <summary>
    /// Timestamp when the event was created in the database
    /// </summary>
    [Column("created_at")]
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity representing a replay operation
/// </summary>
[Table("replay_operations")]
public class ReplayOperationEntity
{
    /// <summary>
    /// Primary key (auto-generated)
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Unique identifier for the replay operation
    /// </summary>
    [Column("replay_id")]
    [Required]
    public Guid ReplayId { get; set; }

    /// <summary>
    /// Status of the replay operation
    /// </summary>
    [Column("status")]
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Filters applied to the replay as JSON
    /// </summary>
    [Column("filters", TypeName = "jsonb")]
    [Required]
    public string Filters { get; set; } = string.Empty;

    /// <summary>
    /// Filter criteria as JSON
    /// </summary>
    [Column("filter_criteria", TypeName = "jsonb")]
    public string? FilterCriteria { get; set; }

    /// <summary>
    /// Destination configuration as JSON
    /// </summary>
    [Column("destination", TypeName = "jsonb")]
    [Required]
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Progress information as JSON
    /// </summary>
    [Column("progress", TypeName = "jsonb")]
    [Required]
    public string Progress { get; set; } = "{}";

    /// <summary>
    /// Timestamp when the replay was created
    /// </summary>
    [Column("created_at")]
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the replay was started
    /// </summary>
    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the replay was completed
    /// </summary>
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Timestamp when the replay was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Error message if the replay failed
    /// </summary>
    [Column("error_message")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Entity representing a dead letter queue event
/// </summary>
[Table("dead_letter_events")]
public class DeadLetterEventEntity
{
    /// <summary>
    /// Primary key (auto-generated)
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Original Kafka topic name
    /// </summary>
    [Column("original_topic")]
    [Required]
    [MaxLength(255)]
    public string OriginalTopic { get; set; } = string.Empty;

    /// <summary>
    /// Original Kafka partition
    /// </summary>
    [Column("original_partition")]
    public int? OriginalPartition { get; set; }

    /// <summary>
    /// Original Kafka offset
    /// </summary>
    [Column("original_offset")]
    public long? OriginalOffset { get; set; }

    /// <summary>
    /// Original message key
    /// </summary>
    [Column("original_key")]
    public string? OriginalKey { get; set; }

    /// <summary>
    /// Original message value
    /// </summary>
    [Column("original_value")]
    public string? OriginalValue { get; set; }

    /// <summary>
    /// Original message headers as JSON
    /// </summary>
    [Column("original_headers", TypeName = "jsonb")]
    public string? OriginalHeaders { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    [Column("error_message")]
    [Required]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error stack trace
    /// </summary>
    [Column("error_stack_trace")]
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    [Column("retry_count")]
    [Required]
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    [Column("created_at")]
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of the last retry attempt
    /// </summary>
    [Column("last_retry_at")]
    public DateTime? LastRetryAt { get; set; }

    /// <summary>
    /// Timestamp when the event was resolved
    /// </summary>
    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// User who resolved the event
    /// </summary>
    [Column("resolved_by")]
    [MaxLength(255)]
    public string? ResolvedBy { get; set; }
}

/// <summary>
/// Entity representing a dead letter queue item
/// </summary>
[Table("dead_letter_queue")]
public class DeadLetterQueueEntity
{
    /// <summary>
    /// Primary key (auto-generated)
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Original message key
    /// </summary>
    [Column("event_key")]
    [MaxLength(255)]
    public string? EventKey { get; set; }

    /// <summary>
    /// Original event payload as JSON
    /// </summary>
    [Column("event_payload", TypeName = "jsonb")]
    public string? EventPayload { get; set; }

    /// <summary>
    /// Reason for failure
    /// </summary>
    [Column("failure_reason")]
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Detailed error information
    /// </summary>
    [Column("error_details")]
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Original message that failed processing
    /// </summary>
    [Column("original_message")]
    [Required]
    public string OriginalMessage { get; set; } = string.Empty;

    /// <summary>
    /// Original message payload
    /// </summary>
    [Column("original_payload", TypeName = "jsonb")]
    public string? OriginalPayload { get; set; }

    /// <summary>
    /// Original Kafka topic name
    /// </summary>
    [Column("original_topic")]
    [Required]
    [MaxLength(255)]
    public string OriginalTopic { get; set; } = string.Empty;

    /// <summary>
    /// Error type/category
    /// </summary>
    [Column("error_type")]
    [Required]
    [MaxLength(255)]
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Error message describing why processing failed
    /// </summary>
    [Column("error_message")]
    [Required]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Exception details if available
    /// </summary>
    [Column("exception_details")]
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// Kafka topic the message came from
    /// </summary>
    [Column("topic")]
    [Required]
    [MaxLength(255)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Kafka partition the message came from
    /// </summary>
    [Column("partition")]
    [Required]
    public int Partition { get; set; }

    /// <summary>
    /// Kafka offset of the message
    /// </summary>
    [Column("offset")]
    [Required]
    public long Offset { get; set; }

    /// <summary>
    /// Number of times processing was retried
    /// </summary>
    [Column("retry_count")]
    [Required]
    public int RetryCount { get; set; }

    /// <summary>
    /// Timestamp when the message was first processed
    /// </summary>
    [Column("first_processed_at")]
    [Required]
    public DateTime FirstProcessedAt { get; set; }

    /// <summary>
    /// Timestamp when the message was moved to DLQ
    /// </summary>
    [Column("dlq_timestamp")]
    [Required]
    public DateTime DlqTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    [Column("created_at")]
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the message has been reviewed
    /// </summary>
    [Column("reviewed")]
    [Required]
    public bool Reviewed { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    [Column("status")]
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "failed";

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }
}

/// <summary>
/// Entity representing system metrics
/// </summary>
[Table("metrics")]
public class MetricsEntity
{
    /// <summary>
    /// Primary key (auto-generated)
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Metric name
    /// </summary>
    [Column("metric_name")]
    [Required]
    [MaxLength(255)]
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Metric value
    /// </summary>
    [Column("value")]
    [Required]
    public double Value { get; set; }

    /// <summary>
    /// Metric value
    /// </summary>
    [Column("metric_value")]
    [Required]
    public double MetricValue { get; set; }

    /// <summary>
    /// Metric unit
    /// </summary>
    [Column("unit")]
    [MaxLength(50)]
    public string? Unit { get; set; }

    /// <summary>
    /// Metric unit
    /// </summary>
    [Column("metric_unit")]
    [MaxLength(50)]
    public string? MetricUnit { get; set; }

    /// <summary>
    /// Metric type (counter, gauge, histogram, summary)
    /// </summary>
    [Column("metric_type")]
    [Required]
    [MaxLength(50)]
    public string MetricType { get; set; } = string.Empty;

    /// <summary>
    /// Metric tags as JSON
    /// </summary>
    [Column("tags", TypeName = "jsonb")]
    public string? Tags { get; set; }

    /// <summary>
    /// Metric labels as JSON
    /// </summary>
    [Column("labels", TypeName = "jsonb")]
    public string? Labels { get; set; }

    /// <summary>
    /// Service that generated the metric
    /// </summary>
    [Column("service_name")]
    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the metric was recorded
    /// </summary>
    [Column("timestamp")]
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }
}

/// <summary>
/// Entity representing audit log entries
/// </summary>
[Table("audit_log")]
public class AuditLogEntity
{
    /// <summary>
    /// Primary key (auto-generated)
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Event ID associated with this audit log entry
    /// </summary>
    [Column("event_id")]
    public Guid? EventId { get; set; }

    /// <summary>
    /// Entity type (alias for ResourceType)
    /// </summary>
    [NotMapped]
    public string? EntityType 
    { 
        get => ResourceType; 
        set => ResourceType = value; 
    }

    /// <summary>
    /// Entity ID (alias for ResourceId)
    /// </summary>
    [NotMapped]
    public string? EntityId 
    { 
        get => ResourceId; 
        set => ResourceId = value; 
    }

    /// <summary>
    /// User ID who performed the action
    /// </summary>
    [Column("user_id")]
    [MaxLength(255)]
    public string? UserId { get; set; }

    /// <summary>
    /// Action performed
    /// </summary>
    [Column("action")]
    [Required]
    [MaxLength(255)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Resource that was accessed
    /// </summary>
    [Column("resource")]
    [Required]
    [MaxLength(255)]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Resource ID that was accessed
    /// </summary>
    [Column("resource_id")]
    [MaxLength(255)]
    public string? ResourceId { get; set; }

    /// <summary>
    /// Resource type that was accessed
    /// </summary>
    [Column("resource_type")]
    [MaxLength(255)]
    public string? ResourceType { get; set; }

    /// <summary>
    /// Request data as JSON
    /// </summary>
    [Column("request_data", TypeName = "jsonb")]
    public string? RequestData { get; set; }

    /// <summary>
    /// Details as JSON
    /// </summary>
    [Column("details", TypeName = "jsonb")]
    public string? Details { get; set; }

    /// <summary>
    /// HTTP response status code
    /// </summary>
    [Column("response_status")]
    public int? ResponseStatus { get; set; }

    /// <summary>
    /// IP address of the request
    /// </summary>
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the request
    /// </summary>
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Timestamp when the action was performed
    /// </summary>
    [Column("timestamp")]
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
