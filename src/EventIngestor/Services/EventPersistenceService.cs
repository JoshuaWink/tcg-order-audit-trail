using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Data;
using OrderAuditTrail.Shared.Data.Repositories;
using OrderAuditTrail.Shared.Events;
using OrderAuditTrail.Shared.Models;
using System.Text.Json;

namespace OrderAuditTrail.EventIngestor.Services;

public interface IEventPersistenceService
{
    Task<bool> IsDuplicateEventAsync(IEvent eventData, CancellationToken cancellationToken);
    Task<PersistenceResult> PersistEventAsync(IEvent eventData, string topic, int partition, long offset, CancellationToken cancellationToken);
}

public class PersistenceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long? EventId { get; set; }
}

public class EventPersistenceService : IEventPersistenceService
{
    private readonly ILogger<EventPersistenceService> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly AuditDbContext _context;

    public EventPersistenceService(
        ILogger<EventPersistenceService> logger,
        IEventRepository eventRepository,
        IAuditLogRepository auditLogRepository,
        AuditDbContext context)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _auditLogRepository = auditLogRepository;
        _context = context;
    }

    public async Task<bool> IsDuplicateEventAsync(IEvent eventData, CancellationToken cancellationToken)
    {
        try
        {
            var existingEvent = await _eventRepository.GetByEventIdAsync(eventData.EventId, cancellationToken);
            return existingEvent != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate event {EventId}", eventData.EventId);
            // In case of error, assume it's not a duplicate to avoid losing events
            return false;
        }
    }

    public async Task<PersistenceResult> PersistEventAsync(
        IEvent eventData, 
        string topic, 
        int partition, 
        long offset, 
        CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Create the event entity
            var eventEntity = new EventEntity
            {
                EventId = eventData.EventId,
                EventType = eventData.EventType,
                AggregateId = eventData.AggregateId,
                AggregateType = eventData.AggregateType,
                Version = eventData.Version,
                Timestamp = eventData.Timestamp,
                Source = eventData.Source,
                Topic = topic,
                Partition = partition,
                Offset = offset,
                EventData = JsonSerializer.Serialize(eventData),
                CreatedAt = DateTime.UtcNow,
                CorrelationId = eventData.CorrelationId,
                CausationId = eventData.CausationId,
                UserId = eventData.UserId
            };

            // Save the event
            var savedEvent = await _eventRepository.AddAsync(eventEntity, cancellationToken);
            
            // Create audit log entry
            var auditLog = new AuditLogEntity
            {
                EventId = eventData.EventId,
                Action = "EVENT_INGESTED",
                EntityType = eventData.AggregateType,
                EntityId = eventData.AggregateId,
                UserId = eventData.UserId,
                Timestamp = DateTime.UtcNow,
                Details = $"Event {eventData.EventType} ingested from topic {topic}",
                IpAddress = null, // Not available in this context
                UserAgent = eventData.Source
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            // Commit the transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug("Successfully persisted event {EventType} with ID {EventId} to database", 
                eventData.EventType, eventData.EventId);

            return new PersistenceResult
            {
                Success = true,
                EventId = savedEvent.Id
            };
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Database error persisting event {EventType} with ID {EventId}", 
                eventData.EventType, eventData.EventId);

            return new PersistenceResult
            {
                Success = false,
                ErrorMessage = $"Database error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Unexpected error persisting event {EventType} with ID {EventId}", 
                eventData.EventType, eventData.EventId);

            return new PersistenceResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }
}
