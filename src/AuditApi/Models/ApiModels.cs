using System.ComponentModel.DataAnnotations;

namespace OrderAuditTrail.AuditApi.Models;

// Base Response Models
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

// Event DTOs
public class EventDto
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int Partition { get; set; }
    public long Offset { get; set; }
    public string EventData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public string? UserId { get; set; }
}

public class EventSummaryDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

// Audit Log DTOs
public class AuditLogDto
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

// Metrics DTOs
public class MetricsDto
{
    public long Id { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public double MetricValue { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class EventMetricsDto
{
    public string EventType { get; set; } = string.Empty;
    public long Count { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
    public double AveragePerHour { get; set; }
}

// Dead Letter Queue DTOs
public class DeadLetterQueueDto
{
    public long Id { get; set; }
    public string? EventKey { get; set; }
    public string EventPayload { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int Partition { get; set; }
    public long Offset { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastRetryAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
}

// Event Replay DTOs
public class EventReplayDto
{
    public long Id { get; set; }
    public string ReplayId { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public DateTime FromTimestamp { get; set; }
    public DateTime ToTimestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorDetails { get; set; }
    public int EventsReplayed { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

// Request Models
public class EventQueryRequest
{
    public string? EventType { get; set; }
    public string? AggregateId { get; set; }
    public string? AggregateType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? UserId { get; set; }
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; } = "timestamp";
    public string? SortDirection { get; set; } = "desc";
}

public class AuditLogQueryRequest
{
    public string? Action { get; set; }
    public string? UserId { get; set; }
    public string? Resource { get; set; }
    public string? ResourceType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? IpAddress { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; } = "timestamp";
    public string? SortDirection { get; set; } = "desc";
}

public class EventReplayRequest
{
    [Required]
    public string ReplayName { get; set; } = string.Empty;
    
    [Required]
    public DateTime FromDate { get; set; }
    
    [Required]
    public DateTime ToDate { get; set; }
    
    public string? EventType { get; set; }
    public string? AggregateId { get; set; }
    public string? AggregateType { get; set; }
    
    [Required]
    public string Destination { get; set; } = string.Empty;
    
    public string? RequestedBy { get; set; }
    public Dictionary<string, string>? Filters { get; set; }
}

public class MetricsQueryRequest
{
    public string? MetricName { get; set; }
    public string? ServiceName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? MetricType { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; } = "timestamp";
    public string? SortDirection { get; set; } = "desc";
}

// Statistics Models
public class EventStatisticsDto
{
    public long TotalEvents { get; set; }
    public long TotalUniqueAggregates { get; set; }
    public DateTime OldestEvent { get; set; }
    public DateTime NewestEvent { get; set; }
    public Dictionary<string, long> EventsByType { get; set; } = new();
    public Dictionary<string, long> EventsBySource { get; set; } = new();
    public Dictionary<string, long> EventsByHour { get; set; } = new();
    public Dictionary<string, long> EventsByAggregateType { get; set; } = new();
}

public class SystemHealthDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> ComponentStatus { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public long EventsProcessedLast24h { get; set; }
    public long DeadLetterQueueSize { get; set; }
    public double AverageProcessingTime { get; set; }
}
