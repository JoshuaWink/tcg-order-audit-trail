namespace OrderAuditTrail.Shared.Events;

/// <summary>
/// Base interface for all events in the system
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Type of the event (e.g., "OrderCreated", "PaymentProcessed")
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Identifier of the aggregate that generated this event
    /// </summary>
    string AggregateId { get; }

    /// <summary>
    /// Type of the aggregate (e.g., "Order", "Payment")
    /// </summary>
    string AggregateType { get; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Version of the event within the aggregate stream
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Event metadata including correlation IDs and source information
    /// </summary>
    EventMetadata Metadata { get; }

    /// <summary>
    /// Source service or system that generated this event
    /// </summary>
    string? Source { get; }

    /// <summary>
    /// Correlation ID to trace related events across services
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Causation ID linking this event to the command that caused it
    /// </summary>
    string? CausationId { get; }

    /// <summary>
    /// ID of the user who triggered this event
    /// </summary>
    string? UserId { get; }
}

/// <summary>
/// Base abstract class for all events
/// </summary>
public abstract class BaseEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public abstract string EventType { get; }
    public required string AggregateId { get; init; }
    public abstract string AggregateType { get; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public int Version { get; init; } = 1;
    public EventMetadata Metadata { get; init; } = new();

    // Convenience properties that delegate to Metadata
    public string? Source => Metadata.Source;
    public string? CorrelationId => Metadata.CorrelationId;
    public string? CausationId => Metadata.CausationId;
    public string? UserId => Metadata.UserId;
}

/// <summary>
/// Event metadata containing correlation IDs and source information
/// </summary>
public class EventMetadata
{
    /// <summary>
    /// Correlation ID to trace related events across services
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Causation ID linking this event to the command that caused it
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// ID of the user who triggered this event
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Source service or system that generated this event
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Additional custom metadata
    /// </summary>
    public Dictionary<string, string> Custom { get; init; } = new();
}

/// <summary>
/// Interface for events that can be serialized to/from JSON
/// </summary>
public interface ISerializableEvent : IEvent
{
    /// <summary>
    /// Serialize the event payload to JSON
    /// </summary>
    string ToJson();

    /// <summary>
    /// Get the event payload as a dictionary for flexible serialization
    /// </summary>
    Dictionary<string, object?> GetPayload();
}

/// <summary>
/// Generic base class for domain events with strongly-typed payload
/// </summary>
/// <typeparam name="TPayload">The type of the event payload</typeparam>
public abstract class DomainEvent<TPayload> : BaseEvent, ISerializableEvent
    where TPayload : class
{
    /// <summary>
    /// The event payload containing the event-specific data
    /// </summary>
    public required TPayload Payload { get; init; }

    /// <summary>
    /// Serialize the event payload to JSON
    /// </summary>
    public virtual string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(Payload, JsonSerializerOptions);
    }

    /// <summary>
    /// Get the event payload as a dictionary
    /// </summary>
    public virtual Dictionary<string, object?> GetPayload()
    {
        var json = ToJson();
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonSerializerOptions) 
               ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// JSON serialization options used for consistent serialization
    /// </summary>
    protected virtual System.Text.Json.JsonSerializerOptions JsonSerializerOptions => new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}

/// <summary>
/// Interface for event handlers that can process events
/// </summary>
/// <typeparam name="TEvent">The type of event to handle</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Handle the event asynchronously
    /// </summary>
    /// <param name="event">The event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for event publishers that can publish events
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish an event to the event stream
    /// </summary>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish multiple events to the event stream
    /// </summary>
    /// <param name="events">The events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for event stores that can persist and retrieve events
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Save an event to the event store
    /// </summary>
    /// <param name="event">The event to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SaveAsync(IEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save multiple events to the event store
    /// </summary>
    /// <param name="events">The events to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SaveAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events for a specific aggregate
    /// </summary>
    /// <param name="aggregateType">The type of aggregate</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="fromVersion">Starting version (inclusive)</param>
    /// <param name="toVersion">Ending version (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The events for the aggregate</returns>
    Task<IEnumerable<IEvent>> GetEventsAsync(
        string aggregateType, 
        string aggregateId, 
        int fromVersion = 1, 
        int? toVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events by type within a date range
    /// </summary>
    /// <param name="eventType">The type of events to retrieve</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The events matching the criteria</returns>
    Task<IEnumerable<IEvent>> GetEventsByTypeAsync(
        string eventType, 
        DateTime? startDate = null, 
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
