using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Data.Repositories;
using OrderAuditTrail.AuditApi.Models;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.AuditApi.Services;

public class MetricsQueryService : IMetricsQueryService
{
    private readonly IMetricsRepository _metricsRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IDeadLetterQueueRepository _dlqRepository;
    private readonly ILogger<MetricsQueryService> _logger;

    public MetricsQueryService(
        IMetricsRepository metricsRepository,
        IEventRepository eventRepository,
        IDeadLetterQueueRepository dlqRepository,
        ILogger<MetricsQueryService> logger)
    {
        _metricsRepository = metricsRepository;
        _eventRepository = eventRepository;
        _dlqRepository = dlqRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<MetricsDto>> GetMetricsAsync(MetricsQueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryable = _metricsRepository.GetQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.MetricName))
            {
                queryable = queryable.Where(m => m.MetricName == request.MetricName);
            }

            if (request.FromTimestamp.HasValue)
            {
                queryable = queryable.Where(m => m.Timestamp >= request.FromTimestamp.Value);
            }

            if (request.ToTimestamp.HasValue)
            {
                queryable = queryable.Where(m => m.Timestamp <= request.ToTimestamp.Value);
            }

            // Apply tag filters
            if (request.Tags != null && request.Tags.Any())
            {
                foreach (var tag in request.Tags)
                {
                    queryable = queryable.Where(m => m.Tags.ContainsKey(tag.Key) && m.Tags[tag.Key] == tag.Value);
                }
            }

            // Order by timestamp descending
            queryable = queryable.OrderByDescending(m => m.Timestamp);

            var totalCount = await queryable.CountAsync(cancellationToken);

            var metrics = await queryable
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var metricsDtos = metrics.Select(MapToDto).ToList();

            return new PaginatedResponse<MetricsDto>
            {
                Items = metricsDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying metrics with request: {@Request}", request);
            throw;
        }
    }

    public async Task<List<EventMetricsDto>> GetEventMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var fromTimestamp = fromDate ?? DateTime.UtcNow.AddDays(-7);
            var toTimestamp = toDate ?? DateTime.UtcNow;

            var eventMetrics = await _eventRepository.GetQueryable()
                .Where(e => e.Timestamp >= fromTimestamp && e.Timestamp <= toTimestamp)
                .GroupBy(e => e.EventType)
                .Select(g => new EventMetricsDto
                {
                    EventType = g.Key,
                    Count = g.Count(),
                    FirstOccurrence = g.Min(e => e.Timestamp),
                    LastOccurrence = g.Max(e => e.Timestamp),
                    AveragePerHour = g.Count() / (double)((toTimestamp - fromTimestamp).TotalHours)
                })
                .OrderByDescending(m => m.Count)
                .ToListAsync(cancellationToken);

            return eventMetrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event metrics");
            throw;
        }
    }

    public async Task<SystemHealthDto> GetSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);

            // Get basic metrics
            var eventsProcessedLast24h = await _eventRepository.GetQueryable()
                .Where(e => e.CreatedAt >= last24Hours)
                .CountAsync(cancellationToken);

            var dlqSize = await _dlqRepository.GetQueryable()
                .Where(d => d.Status == "PENDING")
                .CountAsync(cancellationToken);

            // Calculate average processing time (simulated)
            var averageProcessingTime = await CalculateAverageProcessingTimeAsync(cancellationToken);

            return new SystemHealthDto
            {
                Status = "Healthy",
                Timestamp = now,
                ComponentStatus = new Dictionary<string, string>
                {
                    ["Database"] = "Healthy",
                    ["Redis"] = "Healthy",
                    ["Kafka"] = "Healthy"
                },
                Metrics = new Dictionary<string, object>
                {
                    ["DatabaseConnections"] = await GetDatabaseConnectionCountAsync(cancellationToken),
                    ["MemoryUsage"] = GC.GetTotalMemory(false),
                    ["ThreadCount"] = System.Diagnostics.Process.GetCurrentProcess().Threads.Count
                },
                EventsProcessedLast24h = eventsProcessedLast24h,
                DeadLetterQueueSize = dlqSize,
                AverageProcessingTime = averageProcessingTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system metrics");
            throw;
        }
    }

    public async Task<EventStatisticsDto> GetEventMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventMetrics = await _eventRepository.GetQueryable()
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .GroupBy(e => e.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventType, x => (long)x.Count, cancellationToken);

            var totalEvents = await _eventRepository.GetQueryable()
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .CountAsync(cancellationToken);

            var eventsPerHour = await _eventRepository.GetQueryable()
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .GroupBy(e => new { e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour })
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => $"{x.Hour.Year:D4}-{x.Hour.Month:D2}-{x.Hour.Day:D2} {x.Hour.Hour:D2}:00", x => (long)x.Count, cancellationToken);

            var uniqueAggregates = await _eventRepository.GetQueryable()
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .Select(e => e.AggregateId)
                .Distinct()
                .CountAsync(cancellationToken);

            var oldestEvent = await _eventRepository.GetQueryable()
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .OrderBy(e => e.Timestamp)
                .Select(e => e.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            var newestEvent = await _eventRepository.GetQueryable()
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .OrderByDescending(e => e.Timestamp)
                .Select(e => e.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            return new EventStatisticsDto
            {
                TotalEvents = totalEvents,
                TotalUniqueAggregates = uniqueAggregates,
                OldestEvent = oldestEvent == default ? startDate : oldestEvent,
                NewestEvent = newestEvent == default ? endDate : newestEvent,
                EventsByType = eventMetrics,
                EventsBySource = new Dictionary<string, long>(),
                EventsByHour = eventsPerHour,
                EventsByAggregateType = new Dictionary<string, long>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event metrics");
            throw;
        }
    }

    public async Task<OrderMetrics?> GetOrderMetricsAsync(string orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var orderEvents = await _eventRepository.GetQueryable()
                .Where(e => e.AggregateId == orderId && e.AggregateType == "Order")
                .ToListAsync(cancellationToken);

            if (!orderEvents.Any())
                return null;

            var eventsByType = orderEvents
                .GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            var firstEvent = orderEvents.OrderBy(e => e.Timestamp).First();
            var lastEvent = orderEvents.OrderByDescending(e => e.Timestamp).First();

            return new OrderMetrics
            {
                OrderId = orderId,
                TotalEvents = orderEvents.Count,
                EventsByType = eventsByType,
                FirstEventTime = firstEvent.Timestamp,
                LastEventTime = lastEvent.Timestamp,
                ProcessingDuration = lastEvent.Timestamp - firstEvent.Timestamp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order metrics for {OrderId}", orderId);
            throw;
        }
    }

    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);

            // Get processing time metrics
            var processingTimes = await _metricsRepository.GetQueryable()
                .Where(m => m.MetricName == "events_processing_duration_seconds" &&
                           m.Timestamp >= last24Hours)
                .Select(m => m.MetricValue)
                .ToListAsync(cancellationToken);

            var averageProcessingTime = processingTimes.Any() ? processingTimes.Average() * 1000 : 0;
            var maxProcessingTime = processingTimes.Any() ? processingTimes.Max() * 1000 : 0;
            var minProcessingTime = processingTimes.Any() ? processingTimes.Min() * 1000 : 0;

            // Get throughput metrics
            var eventsProcessed = await _eventRepository.GetQueryable()
                .Where(e => e.CreatedAt >= last24Hours)
                .CountAsync(cancellationToken);

            var eventsPerSecond = eventsProcessed / (24.0 * 3600.0); // Events per second over 24 hours

            // Get error rates
            var errorCount = await _dlqRepository.GetQueryable()
                .Where(d => d.CreatedAt >= last24Hours)
                .CountAsync(cancellationToken);

            var errorRate = eventsProcessed > 0 ? (errorCount / (double)eventsProcessed) * 100 : 0;

            return new PerformanceMetrics
            {
                AverageProcessingTime = averageProcessingTime,
                MaxProcessingTime = maxProcessingTime,
                MinProcessingTime = minProcessingTime,
                EventsPerSecond = eventsPerSecond,
                ErrorRate = errorRate,
                TotalEventsProcessed = eventsProcessed,
                TotalErrors = errorCount,
                Timestamp = now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            throw;
        }
    }

    private async Task<double> CalculateAverageProcessingTimeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            
            var processingTimeMetrics = await _metricsRepository.GetQueryable()
                .Where(m => m.MetricName == "events_processing_duration_seconds" &&
                           m.Timestamp >= last24Hours)
                .ToListAsync(cancellationToken);

            if (!processingTimeMetrics.Any())
            {
                return 0;
            }

            return processingTimeMetrics.Average(m => m.MetricValue) * 1000; // Convert to milliseconds
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetDatabaseConnectionCountAsync(CancellationToken cancellationToken)
    {
        try
        {
            // This is a simplified example - in a real implementation,
            // you would query the database for actual connection counts
            return 10;
        }
        catch
        {
            return 0;
        }
    }

    private static MetricsDto MapToDto(MetricsEntity entity)
    {
        return new MetricsDto
        {
            Id = entity.Id,
            MetricName = entity.MetricName,
            MetricValue = entity.MetricValue,
            Timestamp = entity.Timestamp,
            Tags = entity.Tags ?? new Dictionary<string, string>()
        };
    }
}
