using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Data.Repositories;
using OrderAuditTrail.AuditApi.Models;
using System.Linq.Expressions;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.AuditApi.Services;

public class EventQueryService : IEventQueryService
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventQueryService> _logger;

    public EventQueryService(IEventRepository eventRepository, ILogger<EventQueryService> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<EventDto>> GetEventsAsync(OrderAuditTrail.AuditApi.Models.EventQueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var (events, totalCount) = await _eventRepository.SearchEventsAsync(
                aggregateId: request.AggregateId,
                aggregateType: request.AggregateType,
                eventType: request.EventType,
                fromDate: request.FromDate,
                toDate: request.ToDate,
                pageSize: request.PageSize,
                pageNumber: request.PageNumber,
                cancellationToken: cancellationToken);

            var eventDtos = events.Select(MapToDto).ToList();

            return new PaginatedResponse<EventDto>
            {
                Items = eventDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying events with request: {@Request}", request);
            throw;
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventEntity = await _eventRepository.GetByEventIdAsync(id, cancellationToken);
            return eventEntity != null ? MapToDto(eventEntity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event by ID: {EventId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<EventDto>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _eventRepository.GetEventsForAggregateAsync(aggregateId, cancellationToken: cancellationToken);
            return events.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events by aggregate ID: {AggregateId}", aggregateId);
            throw;
        }
    }

    public async Task<IEnumerable<EventDto>> GetEventsByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            var (events, _) = await _eventRepository.GetEventsByTypeAsync(
                eventType, 
                DateTime.UtcNow.AddYears(-1), 
                DateTime.UtcNow,
                pageSize: 1000,
                pageNumber: 1,
                cancellationToken: cancellationToken);
            
            return events.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events by event type: {EventType}", eventType);
            throw;
        }
    }

    public async Task<List<EventDto>> GetEventsByAggregateAsync(string aggregateId, string aggregateType, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _eventRepository.GetByAggregateIdAsync(aggregateId, aggregateType, cancellationToken);
            return events.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events by aggregate: {AggregateId}, {AggregateType}", aggregateId, aggregateType);
            throw;
        }
    }

    public async Task<List<EventDto>> GetEventsByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _eventRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);
            return events.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events by correlation ID: {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<EventStatisticsDto> GetEventStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var fromTimestamp = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var toTimestamp = toDate ?? DateTime.UtcNow;

            var queryable = _eventRepository.GetQueryable()
                .Where(e => e.Timestamp >= fromTimestamp && e.Timestamp <= toTimestamp);

            var totalEvents = await queryable.CountAsync(cancellationToken);
            var uniqueAggregates = await queryable.Select(e => e.AggregateId).Distinct().CountAsync(cancellationToken);

            var oldestEvent = await queryable.MinAsync(e => e.Timestamp, cancellationToken);
            var newestEvent = await queryable.MaxAsync(e => e.Timestamp, cancellationToken);

            var eventsByType = await queryable
                .GroupBy(e => e.EventType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => (long)x.Count, cancellationToken);

            var eventsBySource = await queryable
                .GroupBy(e => e.Source)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Source, x => (long)x.Count, cancellationToken);

            var eventsByAggregateType = await queryable
                .GroupBy(e => e.AggregateType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => (long)x.Count, cancellationToken);

            // Events by hour (last 24 hours)
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var eventsByHour = await queryable
                .Where(e => e.Timestamp >= last24Hours)
                .GroupBy(e => new { Hour = e.Timestamp.Hour, Date = e.Timestamp.Date })
                .Select(g => new { Key = g.Key.Date.AddHours(g.Key.Hour).ToString("yyyy-MM-dd HH:00"), Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => (long)x.Count, cancellationToken);

            return new EventStatisticsDto
            {
                TotalEvents = totalEvents,
                TotalUniqueAggregates = uniqueAggregates,
                OldestEvent = oldestEvent,
                NewestEvent = newestEvent,
                EventsByType = eventsByType,
                EventsBySource = eventsBySource,
                EventsByHour = eventsByHour,
                EventsByAggregateType = eventsByAggregateType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event statistics");
            throw;
        }
    }

    private static IQueryable<EventEntity> ApplySorting(IQueryable<EventEntity> queryable, string? sortBy, bool descending)
    {
        Expression<Func<EventEntity, object>> keySelector = sortBy?.ToLowerInvariant() switch
        {
            "eventtype" => e => e.EventType,
            "aggregateid" => e => e.AggregateId,
            "aggregatetype" => e => e.AggregateType,
            "source" => e => e.Source,
            "createdat" => e => e.CreatedAt,
            "version" => e => e.Version,
            _ => e => e.Timestamp
        };

        return descending ? queryable.OrderByDescending(keySelector) : queryable.OrderBy(keySelector);
    }

    private static EventDto MapToDto(EventEntity entity)
    {
        return new EventDto
        {
            Id = entity.Id,
            EventId = entity.EventId,
            EventType = entity.EventType,
            AggregateId = entity.AggregateId,
            AggregateType = entity.AggregateType,
            Version = entity.Version,
            Timestamp = entity.Timestamp,
            Source = entity.Source,
            Topic = entity.Topic,
            Partition = entity.Partition,
            Offset = entity.Offset,
            EventData = entity.EventData,
            CreatedAt = entity.CreatedAt,
            CorrelationId = entity.CorrelationId,
            CausationId = entity.CausationId,
            UserId = entity.UserId
        };
    }
}
