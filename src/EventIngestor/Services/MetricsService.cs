using OrderAuditTrail.Shared.Data.Repositories;
using OrderAuditTrail.Shared.Models;
using System.Text.Json;

namespace OrderAuditTrail.EventIngestor.Services;

public interface IMetricsService
{
    Task RecordEventProcessedAsync(string eventType);
    Task RecordEventFailureAsync(string topic, string failureReason);
    Task RecordDuplicateEventAsync(string eventType);
}

public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly IMetricsRepository _metricsRepository;

    public MetricsService(
        ILogger<MetricsService> logger,
        IMetricsRepository metricsRepository)
    {
        _logger = logger;
        _metricsRepository = metricsRepository;
    }

    public async Task RecordEventProcessedAsync(string eventType)
    {
        try
        {
            var metric = new MetricsEntity
            {
                MetricName = "events_processed",
                MetricValue = 1,
                Timestamp = DateTime.UtcNow,
                Tags = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["event_type"] = eventType,
                    ["status"] = "success"
                })
            };

            await _metricsRepository.AddAsync(metric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record processed event metric for {EventType}", eventType);
        }
    }

    public async Task RecordEventFailureAsync(string topic, string failureReason)
    {
        try
        {
            var metric = new MetricsEntity
            {
                MetricName = "events_failed",
                MetricValue = 1,
                Timestamp = DateTime.UtcNow,
                Tags = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["topic"] = topic,
                    ["failure_reason"] = failureReason,
                    ["status"] = "failed"
                })
            };

            await _metricsRepository.AddAsync(metric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record failure event metric for topic {Topic}", topic);
        }
    }

    public async Task RecordDuplicateEventAsync(string eventType)
    {
        try
        {
            var metric = new MetricsEntity
            {
                MetricName = "events_duplicate",
                MetricValue = 1,
                Timestamp = DateTime.UtcNow,
                Tags = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["event_type"] = eventType,
                    ["status"] = "duplicate"
                })
            };

            await _metricsRepository.AddAsync(metric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record duplicate event metric for {EventType}", eventType);
        }
    }
}
