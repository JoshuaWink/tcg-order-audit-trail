using OrderAuditTrail.Shared.Data.Repositories;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.EventIngestor.Services;

public interface IDeadLetterQueueService
{
    Task SendToDeadLetterQueueAsync(
        string? eventKey,
        string eventPayload,
        string topic,
        int partition,
        long offset,
        string failureReason,
        string errorDetails);
}

public class DeadLetterQueueService : IDeadLetterQueueService
{
    private readonly ILogger<DeadLetterQueueService> _logger;
    private readonly IDeadLetterQueueRepository _dlqRepository;

    public DeadLetterQueueService(
        ILogger<DeadLetterQueueService> logger,
        IDeadLetterQueueRepository dlqRepository)
    {
        _logger = logger;
        _dlqRepository = dlqRepository;
    }

    public async Task SendToDeadLetterQueueAsync(
        string? eventKey,
        string eventPayload,
        string topic,
        int partition,
        long offset,
        string failureReason,
        string errorDetails)
    {
        try
        {
            var dlqEntry = new DeadLetterQueueEntity
            {
                EventKey = eventKey,
                EventPayload = eventPayload,
                Topic = topic,
                Partition = partition,
                Offset = offset,
                FailureReason = failureReason,
                ErrorDetails = errorDetails,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0,
                Status = "PENDING"
            };

            await _dlqRepository.AddAsync(dlqEntry);

            _logger.LogWarning(
                "Event sent to dead letter queue - Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Reason: {Reason}",
                topic, partition, offset, failureReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to send event to dead letter queue - Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                topic, partition, offset);
            
            // In a production system, you might want to have a fallback mechanism
            // such as writing to a file or another persistent store
        }
    }
}
