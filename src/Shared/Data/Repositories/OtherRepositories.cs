using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.Shared.Data.Repositories;

/// <summary>
/// Entity Framework implementation of the dead letter queue repository
/// </summary>
public class DeadLetterQueueRepository : IDeadLetterQueueRepository
{
    private readonly AuditDbContext _context;

    public DeadLetterQueueRepository(AuditDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DeadLetterQueueEntity> AddAsync(DeadLetterQueueEntity deadLetterMessage, CancellationToken cancellationToken = default)
    {
        if (deadLetterMessage == null)
            throw new ArgumentNullException(nameof(deadLetterMessage));

        deadLetterMessage.CreatedAt = DateTime.UtcNow;
        
        _context.DeadLetterQueue.Add(deadLetterMessage);
        await _context.SaveChangesAsync(cancellationToken);
        
        return deadLetterMessage;
    }

    public async Task<DeadLetterQueueEntity> AddToDeadLetterQueueAsync(
        DeadLetterQueueEntity deadLetterMessage, 
        CancellationToken cancellationToken = default)
    {
        if (deadLetterMessage == null)
            throw new ArgumentNullException(nameof(deadLetterMessage));

        deadLetterMessage.CreatedAt = DateTime.UtcNow;
        
        _context.DeadLetterQueue.Add(deadLetterMessage);
        await _context.SaveChangesAsync(cancellationToken);
        
        return deadLetterMessage;
    }

    public async Task<(IEnumerable<DeadLetterQueueEntity> Messages, int TotalCount)> GetDeadLetterMessagesAsync(
        string? errorType = null,
        string? originalTopic = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        if (pageNumber <= 0)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        var query = _context.DeadLetterQueue.AsQueryable();

        if (!string.IsNullOrEmpty(errorType))
            query = query.Where(d => d.ErrorType == errorType);

        if (!string.IsNullOrEmpty(originalTopic))
            query = query.Where(d => d.OriginalTopic == originalTopic);

        if (fromDate.HasValue)
            query = query.Where(d => d.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(d => d.CreatedAt <= toDate.Value);

        query = query.OrderByDescending(d => d.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var messages = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (messages, totalCount);
    }

    public async Task<DeadLetterQueueEntity?> GetDeadLetterMessageAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.DeadLetterQueue
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteDeadLetterMessageAsync(long id, CancellationToken cancellationToken = default)
    {
        var message = await _context.DeadLetterQueue
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (message == null)
            return false;

        _context.DeadLetterQueue.Remove(message);
        await _context.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}

/// <summary>
/// Entity Framework implementation of the metrics repository
/// </summary>
public class MetricsRepository : IMetricsRepository
{
    private readonly AuditDbContext _context;

    public MetricsRepository(AuditDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<MetricsEntity> AddAsync(MetricsEntity metric, CancellationToken cancellationToken = default)
    {
        if (metric == null)
            throw new ArgumentNullException(nameof(metric));

        metric.Timestamp = DateTime.UtcNow;
        
        _context.Metrics.Add(metric);
        await _context.SaveChangesAsync(cancellationToken);
        
        return metric;
    }

    public async Task<MetricsEntity> RecordMetricAsync(MetricsEntity metric, CancellationToken cancellationToken = default)
    {
        if (metric == null)
            throw new ArgumentNullException(nameof(metric));

        metric.Timestamp = DateTime.UtcNow;
        
        _context.Metrics.Add(metric);
        await _context.SaveChangesAsync(cancellationToken);
        
        return metric;
    }

    public async Task<(IEnumerable<MetricsEntity> Metrics, int TotalCount)> GetMetricsAsync(
        string metricName,
        DateTime fromDate,
        DateTime toDate,
        int pageSize = 1000,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(metricName))
            throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        if (pageNumber <= 0)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        var query = _context.Metrics
            .Where(m => m.MetricName == metricName && 
                       m.Timestamp >= fromDate && 
                       m.Timestamp <= toDate)
            .OrderBy(m => m.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var metrics = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (metrics, totalCount);
    }

    public async Task<(double Sum, double Average, double Min, double Max, int Count)> GetAggregatedMetricsAsync(
        string metricName,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(metricName))
            throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

        var query = _context.Metrics
            .Where(m => m.MetricName == metricName && 
                       m.Timestamp >= fromDate && 
                       m.Timestamp <= toDate);

        var metrics = await query.Select(m => m.Value).ToListAsync(cancellationToken);

        if (!metrics.Any())
            return (0, 0, 0, 0, 0);

        return (
            Sum: metrics.Sum(),
            Average: metrics.Average(),
            Min: metrics.Min(),
            Max: metrics.Max(),
            Count: metrics.Count()
        );
    }

    public IQueryable<MetricsEntity> GetQueryable()
    {
        return _context.Metrics.AsQueryable();
    }
}

/// <summary>
/// Entity Framework implementation of the audit log repository
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly AuditDbContext _context;

    public AuditLogRepository(AuditDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AuditLogEntity> AddAsync(AuditLogEntity auditLog, CancellationToken cancellationToken = default)
    {
        if (auditLog == null)
            throw new ArgumentNullException(nameof(auditLog));

        auditLog.Timestamp = DateTime.UtcNow;
        
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
        
        return auditLog;
    }

    public async Task<AuditLogEntity> AddAuditLogAsync(AuditLogEntity auditLog, CancellationToken cancellationToken = default)
    {
        if (auditLog == null)
            throw new ArgumentNullException(nameof(auditLog));

        auditLog.Timestamp = DateTime.UtcNow;
        
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
        
        return auditLog;
    }

    public async Task<(IEnumerable<AuditLogEntity> Logs, int TotalCount)> GetAuditLogsAsync(
        string? action = null,
        string? userId = null,
        string? resourceType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        if (pageNumber <= 0)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrEmpty(resourceType))
            query = query.Where(a => a.ResourceType == resourceType);

        if (fromDate.HasValue)
            query = query.Where(a => a.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.Timestamp <= toDate.Value);

        query = query.OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var logs = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (logs, totalCount);
    }
}

/// <summary>
/// Entity Framework implementation of the event replay repository
/// </summary>
public class EventReplayRepository : IEventReplayRepository
{
    private readonly AuditDbContext _context;

    public EventReplayRepository(AuditDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ReplayOperationEntity> CreateReplayAsync(ReplayOperationEntity replayOperation, CancellationToken cancellationToken = default)
    {
        if (replayOperation == null)
            throw new ArgumentNullException(nameof(replayOperation));

        replayOperation.CreatedAt = DateTime.UtcNow;
        
        _context.ReplayOperations.Add(replayOperation);
        await _context.SaveChangesAsync(cancellationToken);
        
        return replayOperation;
    }

    public async Task<ReplayOperationEntity?> GetReplayAsync(Guid replayId, CancellationToken cancellationToken = default)
    {
        return await _context.ReplayOperations
            .FirstOrDefaultAsync(r => r.ReplayId == replayId, cancellationToken);
    }

    public async Task<ReplayOperationEntity> UpdateReplayAsync(ReplayOperationEntity replayOperation, CancellationToken cancellationToken = default)
    {
        if (replayOperation == null)
            throw new ArgumentNullException(nameof(replayOperation));

        replayOperation.UpdatedAt = DateTime.UtcNow;
        
        _context.ReplayOperations.Update(replayOperation);
        await _context.SaveChangesAsync(cancellationToken);
        
        return replayOperation;
    }

    public async Task<IEnumerable<ReplayOperationEntity>> GetReplayHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ReplayOperations
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ReplayOperationEntity>> GetActiveReplaysAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ReplayOperations
            .Where(r => r.Status == "PENDING" || r.Status == "RUNNING")
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteReplayAsync(Guid replayId, CancellationToken cancellationToken = default)
    {
        var replay = await _context.ReplayOperations
            .FirstOrDefaultAsync(r => r.ReplayId == replayId, cancellationToken);

        if (replay != null)
        {
            _context.ReplayOperations.Remove(replay);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ReplayOperationEntity?> GetByReplayIdAsync(string replayId, CancellationToken cancellationToken = default)
    {
        return await _context.ReplayOperations
            .FirstOrDefaultAsync(r => r.ReplayId.ToString() == replayId, cancellationToken);
    }

    public async Task<ReplayOperationEntity> AddAsync(ReplayOperationEntity replayOperation, CancellationToken cancellationToken = default)
    {
        if (replayOperation == null)
            throw new ArgumentNullException(nameof(replayOperation));

        replayOperation.CreatedAt = DateTime.UtcNow;
        
        _context.ReplayOperations.Add(replayOperation);
        await _context.SaveChangesAsync(cancellationToken);
        
        return replayOperation;
    }

    public async Task<ReplayOperationEntity> UpdateAsync(ReplayOperationEntity replayOperation, CancellationToken cancellationToken = default)
    {
        if (replayOperation == null)
            throw new ArgumentNullException(nameof(replayOperation));

        replayOperation.UpdatedAt = DateTime.UtcNow;
        
        _context.ReplayOperations.Update(replayOperation);
        await _context.SaveChangesAsync(cancellationToken);
        
        return replayOperation;
    }

    public IQueryable<ReplayOperationEntity> GetQueryable()
    {
        return _context.ReplayOperations.AsQueryable();
    }
}
