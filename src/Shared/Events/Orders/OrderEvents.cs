using OrderAuditTrail.Shared.Events;

namespace OrderAuditTrail.Shared.Events.Orders;

/// <summary>
/// Event fired when a new order is created
/// </summary>
public class OrderCreatedEvent : DomainEvent<OrderCreatedPayload>
{
    public override string EventType => "OrderCreated";
    public override string AggregateType => "Order";
}

/// <summary>
/// Payload for OrderCreated event
/// </summary>
public class OrderCreatedPayload
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required List<OrderItemPayload> Items { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
    public required OrderAddressPayload ShippingAddress { get; init; }
    public OrderAddressPayload? BillingAddress { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Event fired when an order is updated
/// </summary>
public class OrderUpdatedEvent : DomainEvent<OrderUpdatedPayload>
{
    public override string EventType => "OrderUpdated";
    public override string AggregateType => "Order";
}

/// <summary>
/// Payload for OrderUpdated event
/// </summary>
public class OrderUpdatedPayload
{
    public required string OrderId { get; init; }
    public required string UpdatedBy { get; init; }
    public required Dictionary<string, object?> Changes { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event fired when an order is cancelled
/// </summary>
public class OrderCancelledEvent : DomainEvent<OrderCancelledPayload>
{
    public override string EventType => "OrderCancelled";
    public override string AggregateType => "Order";
}

/// <summary>
/// Payload for OrderCancelled event
/// </summary>
public class OrderCancelledPayload
{
    public required string OrderId { get; init; }
    public required string CancelledBy { get; init; }
    public required string Reason { get; init; }
    public decimal? RefundAmount { get; init; }
}

/// <summary>
/// Event fired when an order is completed
/// </summary>
public class OrderCompletedEvent : DomainEvent<OrderCompletedPayload>
{
    public override string EventType => "OrderCompleted";
    public override string AggregateType => "Order";
}

/// <summary>
/// Payload for OrderCompleted event
/// </summary>
public class OrderCompletedPayload
{
    public required string OrderId { get; init; }
    public required DateTime CompletedAt { get; init; }
    public string? CompletedBy { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Represents an order item in the event payload
/// </summary>
public class OrderItemPayload
{
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalPrice { get; init; }
    public string? Sku { get; init; }
    public Dictionary<string, string>? Properties { get; init; }
}

/// <summary>
/// Event fired when an item is added to an order
/// </summary>
public class OrderItemAddedEvent : DomainEvent<OrderItemAddedPayload>
{
    public override string EventType => "OrderItemAdded";
    public override string AggregateType => "Order";
}

/// <summary>
/// Payload for OrderItemAdded event
/// </summary>
public class OrderItemAddedPayload
{
    public required string OrderId { get; init; }
    public required OrderItemPayload Item { get; init; }
    public required string AddedBy { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event fired when an item is removed from an order
/// </summary>
public class OrderItemRemovedEvent : DomainEvent<OrderItemRemovedPayload>
{
    public override string EventType => "OrderItemRemoved";
    public override string AggregateType => "Order";
}

/// <summary>
/// Payload for OrderItemRemoved event
/// </summary>
public class OrderItemRemovedPayload
{
    public required string OrderId { get; init; }
    public required string ProductId { get; init; }
    public required string RemovedBy { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event fired when an order item is updated
/// </summary>
public class OrderItemUpdatedEvent : DomainEvent<OrderItemUpdatedPayload>
{
    public override string EventType => "OrderItemUpdated";
    public override string AggregateType => "Order";
}

/// <summary>
/// Payload for OrderItemUpdated event
/// </summary>
public class OrderItemUpdatedPayload
{
    public required string OrderId { get; init; }
    public required string ProductId { get; init; }
    public required OrderItemPayload OldItem { get; init; }
    public required OrderItemPayload NewItem { get; init; }
    public required string UpdatedBy { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event fired when an order status changes
/// </summary>
public class OrderStatusChangedEvent : DomainEvent<OrderStatusChangedPayload>
{
    public override string EventType => "OrderStatusChanged";
    public override string AggregateType => "Order";
}

/// <summary>
/// Payload for OrderStatusChanged event
/// </summary>
public class OrderStatusChangedPayload
{
    public required string OrderId { get; init; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string ChangedBy { get; init; }
    public string? Reason { get; init; }
    public DateTime StatusChangeTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an address in the event payload
/// </summary>
public class OrderAddressPayload
{
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
    public required string Country { get; init; }
    public string? Street2 { get; init; }
    public string? Company { get; init; }
    public string? Phone { get; init; }
}
