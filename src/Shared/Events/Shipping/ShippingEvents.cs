using OrderAuditTrail.Shared.Events;

namespace OrderAuditTrail.Shared.Events.Shipping;

/// <summary>
/// Event fired when a shipment is created
/// </summary>
public class ShipmentCreatedEvent : DomainEvent<ShipmentCreatedPayload>
{
    public override string EventType => "ShipmentCreated";
    public override string AggregateType => "Shipment";
}

/// <summary>
/// Payload for ShipmentCreated event
/// </summary>
public class ShipmentCreatedPayload
{
    public required string ShipmentId { get; init; }
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required List<ShipmentItemPayload> Items { get; init; }
    public required ShippingAddressPayload ShippingAddress { get; init; }
    public required string Carrier { get; init; }
    public required string ServiceLevel { get; init; }
    public required decimal ShippingCost { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public DateTime? EstimatedDeliveryDate { get; init; }
    public string? TrackingNumber { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a shipment is dispatched
/// </summary>
public class ShipmentDispatchedEvent : DomainEvent<ShipmentDispatchedPayload>
{
    public override string EventType => "ShipmentDispatched";
    public override string AggregateType => "Shipment";
}

/// <summary>
/// Payload for ShipmentDispatched event
/// </summary>
public class ShipmentDispatchedPayload
{
    public required string ShipmentId { get; init; }
    public required string OrderId { get; init; }
    public required string TrackingNumber { get; init; }
    public required string Carrier { get; init; }
    public required DateTime DispatchedAt { get; init; }
    public required string DispatchedBy { get; init; }
    public required string DispatchLocation { get; init; }
    public DateTime? EstimatedDeliveryDate { get; init; }
    public string? DispatchMethod { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a shipment is delivered
/// </summary>
public class ShipmentDeliveredEvent : DomainEvent<ShipmentDeliveredPayload>
{
    public override string EventType => "ShipmentDelivered";
    public override string AggregateType => "Shipment";
}

/// <summary>
/// Payload for ShipmentDelivered event
/// </summary>
public class ShipmentDeliveredPayload
{
    public required string ShipmentId { get; init; }
    public required string OrderId { get; init; }
    public required string TrackingNumber { get; init; }
    public required DateTime DeliveredAt { get; init; }
    public required string DeliveredTo { get; init; }
    public required string DeliveryStatus { get; init; }
    public string? DeliverySignature { get; init; }
    public string? DeliveryLocation { get; init; }
    public string? DeliveryNotes { get; init; }
    public string? DeliveryPhotoUrl { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a shipment tracking is updated
/// </summary>
public class ShipmentTrackingUpdatedEvent : DomainEvent<ShipmentTrackingUpdatedPayload>
{
    public override string EventType => "ShipmentTrackingUpdated";
    public override string AggregateType => "Shipment";
}

/// <summary>
/// Payload for ShipmentTrackingUpdated event
/// </summary>
public class ShipmentTrackingUpdatedPayload
{
    public required string ShipmentId { get; init; }
    public required string TrackingNumber { get; init; }
    public required string Status { get; init; }
    public required string StatusDescription { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required string Location { get; init; }
    public string? Carrier { get; init; }
    public string? CarrierStatus { get; init; }
    public DateTime? EstimatedDeliveryDate { get; init; }
    public string? Notes { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a shipment encounters an exception
/// </summary>
public class ShipmentExceptionEvent : DomainEvent<ShipmentExceptionPayload>
{
    public override string EventType => "ShipmentException";
    public override string AggregateType => "Shipment";
}

/// <summary>
/// Payload for ShipmentException event
/// </summary>
public class ShipmentExceptionPayload
{
    public required string ShipmentId { get; init; }
    public required string TrackingNumber { get; init; }
    public required string ExceptionType { get; init; }
    public required string ExceptionDescription { get; init; }
    public required DateTime ExceptionDate { get; init; }
    public required string Location { get; init; }
    public required string Carrier { get; init; }
    public required string Resolution { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolvedBy { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Represents a shipment item in the event payload
/// </summary>
public class ShipmentItemPayload
{
    public required string ProductId { get; init; }
    public required string Sku { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public decimal? Weight { get; init; }
    public string? SerialNumber { get; init; }
    public Dictionary<string, string>? Properties { get; init; }
}

/// <summary>
/// Event fired when a shipping label is created
/// </summary>
public class ShippingLabelCreatedEvent : DomainEvent<ShippingLabelCreatedPayload>
{
    public override string EventType => "ShippingLabelCreated";
    public override string AggregateType => "Shipment";
}

/// <summary>
/// Payload for ShippingLabelCreated event
/// </summary>
public class ShippingLabelCreatedPayload
{
    public required string ShipmentId { get; init; }
    public required string OrderId { get; init; }
    public required string TrackingNumber { get; init; }
    public required string Carrier { get; init; }
    public required string ServiceLevel { get; init; }
    public required string LabelUrl { get; init; }
    public required decimal ShippingCost { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public string? LabelFormat { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a shipment is cancelled
/// </summary>
public class ShipmentCancelledEvent : DomainEvent<ShipmentCancelledPayload>
{
    public override string EventType => "ShipmentCancelled";
    public override string AggregateType => "Shipment";
}

/// <summary>
/// Payload for ShipmentCancelled event
/// </summary>
public class ShipmentCancelledPayload
{
    public required string ShipmentId { get; init; }
    public required string OrderId { get; init; }
    public required string TrackingNumber { get; init; }
    public required string Reason { get; init; }
    public required DateTime CancelledAt { get; init; }
    public required string CancelledBy { get; init; }
    public decimal? RefundAmount { get; init; }
    public string? CancellationCode { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a shipment is returned
/// </summary>
public class ShipmentReturnedEvent : DomainEvent<ShipmentReturnedPayload>
{
    public override string EventType => "ShipmentReturned";
    public override string AggregateType => "Shipment";
}

/// <summary>
/// Payload for ShipmentReturned event
/// </summary>
public class ShipmentReturnedPayload
{
    public required string ShipmentId { get; init; }
    public required string OrderId { get; init; }
    public required string TrackingNumber { get; init; }
    public required string ReturnReason { get; init; }
    public required DateTime ReturnedAt { get; init; }
    public required string ReturnLocation { get; init; }
    public required string ReturnCondition { get; init; }
    public string? ReturnTrackingNumber { get; init; }
    public string? ReturnCarrier { get; init; }
    public string? ReturnNotes { get; init; }
    public List<ShipmentItemPayload>? ReturnedItems { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Represents a shipping address in the event payload
/// </summary>
public class ShippingAddressPayload
{
    public required string RecipientName { get; init; }
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
    public required string Country { get; init; }
    public string? Street2 { get; init; }
    public string? Company { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? DeliveryInstructions { get; init; }
}
