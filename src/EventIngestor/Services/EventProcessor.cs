using System.Text.Json;
using OrderAuditTrail.Shared.Events;
using OrderAuditTrail.Shared.Events.Orders;
using OrderAuditTrail.Shared.Events.Payments;
using OrderAuditTrail.Shared.Events.Inventory;
using OrderAuditTrail.Shared.Events.Shipping;

namespace OrderAuditTrail.EventIngestor.Services;

public interface IEventProcessor
{
    Task<bool> ProcessEventAsync(
        string? eventKey,
        string eventPayload,
        string topic,
        int partition,
        long offset,
        CancellationToken cancellationToken);
}

public class EventProcessor : IEventProcessor
{
    private readonly ILogger<EventProcessor> _logger;
    private readonly IEventValidationService _validationService;
    private readonly IEventPersistenceService _persistenceService;
    private readonly IDeadLetterQueueService _deadLetterQueueService;
    private readonly IMetricsService _metricsService;

    public EventProcessor(
        ILogger<EventProcessor> logger,
        IEventValidationService validationService,
        IEventPersistenceService persistenceService,
        IDeadLetterQueueService deadLetterQueueService,
        IMetricsService metricsService)
    {
        _logger = logger;
        _validationService = validationService;
        _persistenceService = persistenceService;
        _deadLetterQueueService = deadLetterQueueService;
        _metricsService = metricsService;
    }

    public async Task<bool> ProcessEventAsync(
        string? eventKey,
        string eventPayload,
        string topic,
        int partition,
        long offset,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing event from topic {Topic}: {EventKey}", topic, eventKey);

            // 1. Deserialize the event
            var eventData = await DeserializeEventAsync(eventPayload, topic);
            if (eventData == null)
            {
                await HandleDeserializationFailure(eventKey, eventPayload, topic, partition, offset, "Failed to deserialize event");
                return false;
            }

            // 2. Validate the event
            var validationResult = await _validationService.ValidateEventAsync(eventData, cancellationToken);
            if (!validationResult.IsValid)
            {
                await HandleValidationFailure(eventKey, eventPayload, topic, partition, offset, validationResult.Errors);
                return false;
            }

            // 3. Check for duplicate events
            var isDuplicate = await _persistenceService.IsDuplicateEventAsync(eventData, cancellationToken);
            if (isDuplicate)
            {
                _logger.LogInformation("Duplicate event detected for {EventType} with ID {EventId}, skipping", 
                    eventData.EventType, eventData.EventId);
                await _metricsService.RecordDuplicateEventAsync(eventData.EventType);
                return true; // Consider duplicates as successfully processed
            }

            // 4. Persist the event
            var persistenceResult = await _persistenceService.PersistEventAsync(
                eventData, 
                topic, 
                partition, 
                offset, 
                cancellationToken);

            if (persistenceResult.Success)
            {
                _logger.LogDebug("Successfully persisted event {EventType} with ID {EventId}", 
                    eventData.EventType, eventData.EventId);
                
                await _metricsService.RecordEventProcessedAsync(eventData.EventType);
                return true;
            }
            else
            {
                _logger.LogError("Failed to persist event {EventType} with ID {EventId}: {Error}", 
                    eventData.EventType, eventData.EventId, persistenceResult.ErrorMessage);
                
                await HandlePersistenceFailure(eventKey, eventPayload, topic, partition, offset, persistenceResult.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing event from topic {Topic}", topic);
            await HandleUnexpectedError(eventKey, eventPayload, topic, partition, offset, ex);
            return false;
        }
    }

    private async Task<IEvent?> DeserializeEventAsync(string eventPayload, string topic)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // First, deserialize to get the event type
            using var document = JsonDocument.Parse(eventPayload);
            var eventTypeProperty = document.RootElement.GetProperty("eventType");
            var eventType = eventTypeProperty.GetString();

            if (string.IsNullOrEmpty(eventType))
            {
                _logger.LogError("Event payload missing eventType property");
                return null;
            }

            // Deserialize based on event type
            return eventType switch
            {
                // Order events
                "OrderCreated" => JsonSerializer.Deserialize<OrderCreatedEvent>(eventPayload, options),
                "OrderUpdated" => JsonSerializer.Deserialize<OrderUpdatedEvent>(eventPayload, options),
                "OrderCancelled" => JsonSerializer.Deserialize<OrderCancelledEvent>(eventPayload, options),
                "OrderCompleted" => JsonSerializer.Deserialize<OrderCompletedEvent>(eventPayload, options),
                "OrderItemAdded" => JsonSerializer.Deserialize<OrderItemAddedEvent>(eventPayload, options),
                "OrderItemRemoved" => JsonSerializer.Deserialize<OrderItemRemovedEvent>(eventPayload, options),
                "OrderItemUpdated" => JsonSerializer.Deserialize<OrderItemUpdatedEvent>(eventPayload, options),
                "OrderStatusChanged" => JsonSerializer.Deserialize<OrderStatusChangedEvent>(eventPayload, options),

                // Payment events
                "PaymentInitiated" => JsonSerializer.Deserialize<PaymentInitiatedEvent>(eventPayload, options),
                "PaymentCompleted" => JsonSerializer.Deserialize<PaymentCompletedEvent>(eventPayload, options),
                "PaymentFailed" => JsonSerializer.Deserialize<PaymentFailedEvent>(eventPayload, options),
                "PaymentRefunded" => JsonSerializer.Deserialize<PaymentRefundedEvent>(eventPayload, options),
                "PaymentRefundFailed" => JsonSerializer.Deserialize<PaymentRefundFailedEvent>(eventPayload, options),

                // Inventory events
                "InventoryReserved" => JsonSerializer.Deserialize<InventoryReservedEvent>(eventPayload, options),
                "InventoryReleased" => JsonSerializer.Deserialize<InventoryReleasedEvent>(eventPayload, options),
                "InventoryAllocated" => JsonSerializer.Deserialize<InventoryAllocatedEvent>(eventPayload, options),
                "InventoryUpdated" => JsonSerializer.Deserialize<InventoryUpdatedEvent>(eventPayload, options),
                "InventoryRestocked" => JsonSerializer.Deserialize<InventoryRestockedEvent>(eventPayload, options),

                // Shipping events
                "ShippingLabelCreated" => JsonSerializer.Deserialize<ShippingLabelCreatedEvent>(eventPayload, options),
                "ShipmentCreated" => JsonSerializer.Deserialize<ShipmentCreatedEvent>(eventPayload, options),
                "ShipmentDispatched" => JsonSerializer.Deserialize<ShipmentDispatchedEvent>(eventPayload, options),
                "ShipmentDelivered" => JsonSerializer.Deserialize<ShipmentDeliveredEvent>(eventPayload, options),
                "ShipmentCancelled" => JsonSerializer.Deserialize<ShipmentCancelledEvent>(eventPayload, options),
                "ShipmentReturned" => JsonSerializer.Deserialize<ShipmentReturnedEvent>(eventPayload, options),

                _ => throw new NotSupportedException($"Event type '{eventType}' is not supported")
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize event payload: {Payload}", eventPayload);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deserializing event payload");
            return null;
        }
    }

    private async Task HandleDeserializationFailure(string? eventKey, string eventPayload, string topic, int partition, long offset, string error)
    {
        _logger.LogError("Event deserialization failed for topic {Topic}: {Error}", topic, error);
        
        await _deadLetterQueueService.SendToDeadLetterQueueAsync(
            eventKey,
            eventPayload,
            topic,
            partition,
            offset,
            "DESERIALIZATION_FAILURE",
            error);

        await _metricsService.RecordEventFailureAsync(topic, "DESERIALIZATION_FAILURE");
    }

    private async Task HandleValidationFailure(string? eventKey, string eventPayload, string topic, int partition, long offset, IEnumerable<string> errors)
    {
        var errorMessage = string.Join("; ", errors);
        _logger.LogError("Event validation failed for topic {Topic}: {Errors}", topic, errorMessage);
        
        await _deadLetterQueueService.SendToDeadLetterQueueAsync(
            eventKey,
            eventPayload,
            topic,
            partition,
            offset,
            "VALIDATION_FAILURE",
            errorMessage);

        await _metricsService.RecordEventFailureAsync(topic, "VALIDATION_FAILURE");
    }

    private async Task HandlePersistenceFailure(string? eventKey, string eventPayload, string topic, int partition, long offset, string? error)
    {
        _logger.LogError("Event persistence failed for topic {Topic}: {Error}", topic, error);
        
        await _deadLetterQueueService.SendToDeadLetterQueueAsync(
            eventKey,
            eventPayload,
            topic,
            partition,
            offset,
            "PERSISTENCE_FAILURE",
            error ?? "Unknown persistence error");

        await _metricsService.RecordEventFailureAsync(topic, "PERSISTENCE_FAILURE");
    }

    private async Task HandleUnexpectedError(string? eventKey, string eventPayload, string topic, int partition, long offset, Exception ex)
    {
        _logger.LogError(ex, "Unexpected error processing event from topic {Topic}", topic);
        
        await _deadLetterQueueService.SendToDeadLetterQueueAsync(
            eventKey,
            eventPayload,
            topic,
            partition,
            offset,
            "UNEXPECTED_ERROR",
            ex.Message);

        await _metricsService.RecordEventFailureAsync(topic, "UNEXPECTED_ERROR");
    }
}
