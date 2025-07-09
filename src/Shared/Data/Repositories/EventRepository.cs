using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.Shared.Data.Repositories;

/// <summary>
/// Entity Framework implementation of the event repository
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly AuditDbContext _context;

    public EventRepository(AuditDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<EventEntity> AddAsync(EventEntity eventEntity, CancellationToken cancellationToken = default)
    {
        if (eventEntity == null)
            throw new ArgumentNullException(nameof(eventEntity));

        eventEntity.CreatedAt = DateTime.UtcNow;
        
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync(cancellationToken);
        
        return eventEntity;
    }

    public async Task<EventEntity?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task<EventEntity> AppendEventAsync(EventEntity eventEntity, CancellationToken cancellationToken = default)
    {
        if (eventEntity == null)
            throw new ArgumentNullException(nameof(eventEntity));

        eventEntity.CreatedAt = DateTime.UtcNow;
        
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync(cancellationToken);
        
        return eventEntity;
    }

    public async Task<IEnumerable<EventEntity>> GetEventsForAggregateAsync(
        string aggregateId, 
        int fromVersion = 1, 
        int? toVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(aggregateId))
            throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

        var baseQuery = _context.Events
            .Where(e => e.AggregateId == aggregateId && e.Version >= fromVersion);

        if (toVersion.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Version <= toVersion.Value);
        }

        var query = baseQuery.OrderBy(e => e.Version);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<EventEntity> Events, int TotalCount)> GetEventsByTypeAsync(
        string eventType,
        DateTime fromDate,
        DateTime toDate,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventType))
            throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));

        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        if (pageNumber <= 0)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        var query = _context.Events
            .Where(e => e.EventType == eventType && 
                       e.Timestamp >= fromDate && 
                       e.Timestamp <= toDate)
            .OrderBy(e => e.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var events = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (events, totalCount);
    }

    public async Task<(IEnumerable<EventEntity> Events, int TotalCount)> GetEventsByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        int pageSize = 100,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        if (pageNumber <= 0)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        var query = _context.Events
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
            .OrderBy(e => e.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var events = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (events, totalCount);
    }

    public async Task<EventEntity?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task<int> GetLatestVersionForAggregateAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(aggregateId))
            throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

        var latestVersion = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .MaxAsync(e => (int?)e.Version, cancellationToken);

        return latestVersion ?? 0;
    }

    public async Task<(IEnumerable<EventEntity> Events, int TotalCount)> SearchEventsAsync(
        string? aggregateId = null,
        string? aggregateType = null,
        string? eventType = null,
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

        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrEmpty(aggregateId))
            query = query.Where(e => e.AggregateId == aggregateId);

        if (!string.IsNullOrEmpty(aggregateType))
            query = query.Where(e => e.AggregateType == aggregateType);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(e => e.EventType == eventType);

        if (fromDate.HasValue)
            query = query.Where(e => e.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.Timestamp <= toDate.Value);

        query = query.OrderBy(e => e.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var events = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (events, totalCount);
    }
}

/// <summary>
/// Entity Framework implementation of the replay repository
/// </summary>
public class ReplayRepository : IReplayRepository
{
    private readonly AuditDbContext _context;

    public ReplayRepository(AuditDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ReplayOperationEntity> CreateReplayOperationAsync(
        ReplayOperationEntity replayOperation, 
        CancellationToken cancellationToken = default)
    {
        if (replayOperation == null)
            throw new ArgumentNullException(nameof(replayOperation));

        replayOperation.CreatedAt = DateTime.UtcNow;
        
        _context.ReplayOperations.Add(replayOperation);
        await _context.SaveChangesAsync(cancellationToken);
        
        return replayOperation;
    }

    public async Task<ReplayOperationEntity> UpdateReplayOperationAsync(
        ReplayOperationEntity replayOperation, 
        CancellationToken cancellationToken = default)
    {
        if (replayOperation == null)
            throw new ArgumentNullException(nameof(replayOperation));

        replayOperation.UpdatedAt = DateTime.UtcNow;
        
        _context.ReplayOperations.Update(replayOperation);
        await _context.SaveChangesAsync(cancellationToken);
        
        return replayOperation;
    }

    public async Task<ReplayOperationEntity?> GetReplayOperationAsync(Guid replayId, CancellationToken cancellationToken = default)
    {
        return await _context.ReplayOperations
            .FirstOrDefaultAsync(r => r.ReplayId == replayId, cancellationToken);
    }

    public async Task<(IEnumerable<ReplayOperationEntity> Operations, int TotalCount)> GetReplayOperationsAsync(
        string? status = null,
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

        var query = _context.ReplayOperations.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        if (fromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.CreatedAt <= toDate.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var operations = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (operations, totalCount);
    }
}
