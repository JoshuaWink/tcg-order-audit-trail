using Prometheus;

namespace OrderAuditTrail.EventIngestor.Configuration;

public interface IMetricsCollector
{
    void IncrementProcessedEvents();
    void IncrementProcessingErrors();
    void IncrementConsumeErrors();
    void RecordProcessingTime(TimeSpan processingTime);
}

public class MetricsCollector : IMetricsCollector
{
    private readonly Counter _processedEventsCounter;
    private readonly Counter _processingErrorsCounter;
    private readonly Counter _consumeErrorsCounter;
    private readonly Histogram _processingTimeHistogram;

    public MetricsCollector()
    {
        _processedEventsCounter = Metrics
            .CreateCounter("events_processed_total", "Total number of events processed successfully");

        _processingErrorsCounter = Metrics
            .CreateCounter("events_processing_errors_total", "Total number of event processing errors");

        _consumeErrorsCounter = Metrics
            .CreateCounter("events_consume_errors_total", "Total number of Kafka consume errors");

        _processingTimeHistogram = Metrics
            .CreateHistogram("events_processing_duration_seconds", "Event processing duration in seconds");
    }

    public void IncrementProcessedEvents()
    {
        _processedEventsCounter.Inc();
    }

    public void IncrementProcessingErrors()
    {
        _processingErrorsCounter.Inc();
    }

    public void IncrementConsumeErrors()
    {
        _consumeErrorsCounter.Inc();
    }

    public void RecordProcessingTime(TimeSpan processingTime)
    {
        _processingTimeHistogram.Observe(processingTime.TotalSeconds);
    }
}
