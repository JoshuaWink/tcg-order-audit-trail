using System.ComponentModel.DataAnnotations;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.Shared.Models;

/// <summary>
/// Response model for event queries
/// </summary>
public class EventQueryResponse
{
    public IEnumerable<OrderEvent> Events { get; set; } = new List<OrderEvent>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Event query request model
/// </summary>
public class EventQueryRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? EventType { get; set; }
    public string? OrderId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Base class for order events
/// </summary>
public class OrderEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public int Version { get; set; }
}

/// <summary>
/// Order item model
/// </summary>
public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}

/// <summary>
/// Payment initiated event
/// </summary>
public class PaymentInitiatedEvent : OrderEvent
{
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

/// <summary>
/// Payment completed event
/// </summary>
public class PaymentCompletedEvent : OrderEvent
{
    public string PaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Replay request model
/// </summary>
public class ReplayRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? EventType { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? TargetTopic { get; set; }
    public string? RequestedBy { get; set; }
    public int EstimatedEventCount { get; set; }
}

/// <summary>
/// Replay response model
/// </summary>
public class ReplayResponse
{
    public Guid ReplayId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int EstimatedEventCount { get; set; }
}

/// <summary>
/// Replay status model
/// </summary>
public class ReplayStatus
{
    public Guid ReplayId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EventsReplayed { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Replay cancel result model
/// </summary>
public class ReplayCancelResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Replay history item model
/// </summary>
public class ReplayHistoryItem
{
    public Guid ReplayId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int EventsReplayed { get; set; }
    public string? RequestedBy { get; set; }
}

/// <summary>
/// Event metrics model
/// </summary>
public class EventMetrics
{
    public long TotalEvents { get; set; }
    public Dictionary<string, long> EventsByType { get; set; } = new();
    public Dictionary<string, long> EventsPerHour { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// System metrics model
/// </summary>
public class SystemMetrics
{
    public long EventsProcessedLast24h { get; set; }
    public long DeadLetterQueueSize { get; set; }
    public double AverageProcessingTime { get; set; }
    public int DatabaseConnections { get; set; }
    public long MemoryUsage { get; set; }
    public int ThreadCount { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Order metrics model
/// </summary>
public class OrderMetrics
{
    public string OrderId { get; set; } = string.Empty;
    public long TotalEvents { get; set; }
    public Dictionary<string, long> EventsByType { get; set; } = new();
    public DateTime FirstEventTime { get; set; }
    public DateTime LastEventTime { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
}

/// <summary>
/// Performance metrics model
/// </summary>
public class PerformanceMetrics
{
    public double AverageProcessingTime { get; set; }
    public double MaxProcessingTime { get; set; }
    public double MinProcessingTime { get; set; }
    public double EventsPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public long TotalEventsProcessed { get; set; }
    public long TotalErrors { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Audit log model
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? EventId { get; set; }
    public string? UserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Changes { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalData { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Audit log query request model
/// </summary>
public class AuditLogQueryRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ActionType { get; set; }
    public string? UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Audit log query response model
/// </summary>
public class AuditLogQueryResponse
{
    public IEnumerable<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Create audit log request model
/// </summary>
public class CreateAuditLogRequest
{
    public Guid? EventId { get; set; }
    public string? UserId { get; set; }
    
    [Required]
    public string ActionType { get; set; } = string.Empty;
    
    [Required]
    public string EntityType { get; set; } = string.Empty;
    
    [Required]
    public string EntityId { get; set; } = string.Empty;
    
    public string? Changes { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Audit logs summary model
/// </summary>
public class AuditLogsSummary
{
    public long TotalCount { get; set; }
    public Dictionary<string, long> CountByActionType { get; set; } = new();
    public Dictionary<string, long> CountByUserId { get; set; } = new();
    public IEnumerable<AuditLog> RecentActivity { get; set; } = new List<AuditLog>();
}

/// <summary>
/// Event replay entity model
/// </summary>
public class EventReplayEntity
{
    public long Id { get; set; }
    public Guid ReplayId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Filters { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Progress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
