using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.Shared.Data.Repositories;

/// <summary>
/// Repository interface for event operations
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Adds a new event to the repository
    /// </summary>
    /// <param name="eventEntity">The event to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added event with generated ID</returns>
    Task<EventEntity> AddAsync(EventEntity eventEntity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an event by its event ID
    /// </summary>
    /// <param name="eventId">The event identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The event if found, null otherwise</returns>
    Task<EventEntity?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a new event to the event store
    /// </summary>
    /// <param name="eventEntity">The event to append</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The appended event with generated ID</returns>
    Task<EventEntity> AppendEventAsync(EventEntity eventEntity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events for a specific aggregate
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="fromVersion">Starting version (inclusive)</param>
    /// <param name="toVersion">Ending version (inclusive, null for all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of events for the aggregate</returns>
    Task<IEnumerable<EventEntity>> GetEventsForAggregateAsync(
        string aggregateId, 
        int fromVersion = 1, 
        int? toVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events by type within a date range
    /// </summary>
    /// <param name="eventType">The event type to filter by</param>
    /// <param name="fromDate">Start date (inclusive)</param>
    /// <param name="toDate">End date (inclusive)</param>
    /// <param name="pageSize">Number of events per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of events</returns>
    Task<(IEnumerable<EventEntity> Events, int TotalCount)> GetEventsByTypeAsync(
        string eventType,
        DateTime fromDate,
        DateTime toDate,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events within a date range
    /// </summary>
    /// <param name="fromDate">Start date (inclusive)</param>
    /// <param name="toDate">End date (inclusive)</param>
    /// <param name="pageSize">Number of events per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of events</returns>
    Task<(IEnumerable<EventEntity> Events, int TotalCount)> GetEventsByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific event by ID
    /// </summary>
    /// <param name="eventId">The event identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The event if found, null otherwise</returns>
    Task<EventEntity?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest version for an aggregate
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The latest version, or 0 if no events exist</returns>
    Task<int> GetLatestVersionForAggregateAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches events by multiple criteria
    /// </summary>
    /// <param name="aggregateId">Optional aggregate ID filter</param>
    /// <param name="aggregateType">Optional aggregate type filter</param>
    /// <param name="eventType">Optional event type filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="pageSize">Number of events per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of events matching criteria</returns>
    Task<(IEnumerable<EventEntity> Events, int TotalCount)> SearchEventsAsync(
        string? aggregateId = null,
        string? aggregateType = null,
        string? eventType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for replay operations
/// </summary>
public interface IReplayRepository
{
    /// <summary>
    /// Creates a new replay operation
    /// </summary>
    /// <param name="replayOperation">The replay operation to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created replay operation</returns>
    Task<ReplayOperationEntity> CreateReplayOperationAsync(
        ReplayOperationEntity replayOperation, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing replay operation
    /// </summary>
    /// <param name="replayOperation">The replay operation to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated replay operation</returns>
    Task<ReplayOperationEntity> UpdateReplayOperationAsync(
        ReplayOperationEntity replayOperation, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a replay operation by ID
    /// </summary>
    /// <param name="replayId">The replay operation identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The replay operation if found, null otherwise</returns>
    Task<ReplayOperationEntity?> GetReplayOperationAsync(Guid replayId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all replay operations with optional filtering
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="pageSize">Number of operations per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of replay operations</returns>
    Task<(IEnumerable<ReplayOperationEntity> Operations, int TotalCount)> GetReplayOperationsAsync(
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for dead letter queue operations
/// </summary>
public interface IDeadLetterQueueRepository
{
    /// <summary>
    /// Adds a message to the dead letter queue
    /// </summary>
    /// <param name="deadLetterMessage">The dead letter message to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added dead letter message</returns>
    Task<DeadLetterQueueEntity> AddAsync(DeadLetterQueueEntity deadLetterMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message to the dead letter queue
    /// </summary>
    /// <param name="deadLetterMessage">The dead letter message to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added dead letter message</returns>
    Task<DeadLetterQueueEntity> AddToDeadLetterQueueAsync(
        DeadLetterQueueEntity deadLetterMessage, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves dead letter messages with optional filtering
    /// </summary>
    /// <param name="errorType">Optional error type filter</param>
    /// <param name="originalTopic">Optional original topic filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="pageSize">Number of messages per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of dead letter messages</returns>
    Task<(IEnumerable<DeadLetterQueueEntity> Messages, int TotalCount)> GetDeadLetterMessagesAsync(
        string? errorType = null,
        string? originalTopic = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific dead letter message by ID
    /// </summary>
    /// <param name="id">The dead letter message identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The dead letter message if found, null otherwise</returns>
    Task<DeadLetterQueueEntity?> GetDeadLetterMessageAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a dead letter message (after successful reprocessing)
    /// </summary>
    /// <param name="id">The dead letter message identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteDeadLetterMessageAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for metrics operations
/// </summary>
public interface IMetricsRepository
{
    /// <summary>
    /// Adds a metric entry
    /// </summary>
    /// <param name="metric">The metric entry to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added metric entry</returns>
    Task<MetricsEntity> AddAsync(MetricsEntity metric, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a metric value
    /// </summary>
    /// <param name="metric">The metric to record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The recorded metric</returns>
    Task<MetricsEntity> RecordMetricAsync(MetricsEntity metric, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves metrics by name within a date range
    /// </summary>
    /// <param name="metricName">The metric name</param>
    /// <param name="fromDate">Start date (inclusive)</param>
    /// <param name="toDate">End date (inclusive)</param>
    /// <param name="pageSize">Number of metrics per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of metrics</returns>
    Task<(IEnumerable<MetricsEntity> Metrics, int TotalCount)> GetMetricsAsync(
        string metricName,
        DateTime fromDate,
        DateTime toDate,
        int pageSize = 1000,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregate metrics (sum, avg, min, max) for a metric
    /// </summary>
    /// <param name="metricName">The metric name</param>
    /// <param name="fromDate">Start date (inclusive)</param>
    /// <param name="toDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated metric values</returns>
    Task<(double Sum, double Average, double Min, double Max, int Count)> GetAggregatedMetricsAsync(
        string metricName,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for audit log operations
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Adds an audit log entry
    /// </summary>
    /// <param name="auditLog">The audit log entry to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added audit log entry</returns>
    Task<AuditLogEntity> AddAsync(AuditLogEntity auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an audit log entry
    /// </summary>
    /// <param name="auditLog">The audit log entry to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added audit log entry</returns>
    Task<AuditLogEntity> AddAuditLogAsync(AuditLogEntity auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit logs with optional filtering
    /// </summary>
    /// <param name="action">Optional action filter</param>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="resourceType">Optional resource type filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="pageSize">Number of logs per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of audit logs</returns>
    Task<(IEnumerable<AuditLogEntity> Logs, int TotalCount)> GetAuditLogsAsync(
        string? action = null,
        string? userId = null,
        string? resourceType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for event replay operations
/// </summary>
public interface IEventReplayRepository
{
    /// <summary>
    /// Creates a new replay operation
    /// </summary>
    Task<ReplayOperationEntity> CreateReplayAsync(ReplayOperationEntity replayOperation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a replay operation by ID
    /// </summary>
    Task<ReplayOperationEntity?> GetReplayAsync(Guid replayId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a replay operation
    /// </summary>
    Task<ReplayOperationEntity> UpdateReplayAsync(ReplayOperationEntity replayOperation, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all replay operations
    /// </summary>
    Task<IEnumerable<ReplayOperationEntity>> GetReplayHistoryAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets active replay operations
    /// </summary>
    Task<IEnumerable<ReplayOperationEntity>> GetActiveReplaysAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a replay operation
    /// </summary>
    Task DeleteReplayAsync(Guid replayId, CancellationToken cancellationToken = default);
}
