using OrderAuditTrail.Shared.Events;

namespace OrderAuditTrail.Shared.Events.Inventory;

/// <summary>
/// Event fired when inventory levels are updated
/// </summary>
public class InventoryUpdatedEvent : DomainEvent<InventoryUpdatedPayload>
{
    public override string EventType => "InventoryUpdated";
    public override string AggregateType => "Inventory";
}

/// <summary>
/// Payload for InventoryUpdated event
/// </summary>
public class InventoryUpdatedPayload
{
    public required string ProductId { get; init; }
    public required string Sku { get; init; }
    public required int PreviousQuantity { get; init; }
    public required int NewQuantity { get; init; }
    public required int QuantityChange { get; init; }
    public required string ChangeType { get; init; } // "increase", "decrease", "adjustment"
    public required string Reason { get; init; }
    public required string UpdatedBy { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public string? Location { get; init; }
    public string? BatchNumber { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when inventory is reserved for an order
/// </summary>
public class InventoryReservedEvent : DomainEvent<InventoryReservedPayload>
{
    public override string EventType => "InventoryReserved";
    public override string AggregateType => "Inventory";
}

/// <summary>
/// Payload for InventoryReserved event
/// </summary>
public class InventoryReservedPayload
{
    public required string ProductId { get; init; }
    public required string Sku { get; init; }
    public required string OrderId { get; init; }
    public required int QuantityReserved { get; init; }
    public required int AvailableQuantity { get; init; }
    public required DateTime ReservedAt { get; init; }
    public required string ReservedBy { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? Location { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when inventory is allocated for fulfillment
/// </summary>
public class InventoryAllocatedEvent : DomainEvent<InventoryAllocatedPayload>
{
    public override string EventType => "InventoryAllocated";
    public override string AggregateType => "Inventory";
}

/// <summary>
/// Payload for InventoryAllocated event
/// </summary>
public class InventoryAllocatedPayload
{
    public required string ProductId { get; init; }
    public required string Sku { get; init; }
    public required string OrderId { get; init; }
    public required int QuantityAllocated { get; init; }
    public required int RemainingQuantity { get; init; }
    public required DateTime AllocatedAt { get; init; }
    public required string AllocatedBy { get; init; }
    public string? Location { get; init; }
    public string? BatchNumber { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when inventory reservation is released
/// </summary>
public class InventoryReleasedEvent : DomainEvent<InventoryReleasedPayload>
{
    public override string EventType => "InventoryReleased";
    public override string AggregateType => "Inventory";
}

/// <summary>
/// Payload for InventoryReleased event
/// </summary>
public class InventoryReleasedPayload
{
    public required string ProductId { get; init; }
    public required string Sku { get; init; }
    public required string OrderId { get; init; }
    public required int QuantityReleased { get; init; }
    public required int AvailableQuantity { get; init; }
    public required DateTime ReleasedAt { get; init; }
    public required string ReleasedBy { get; init; }
    public required string Reason { get; init; }
    public string? Location { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when inventory goes out of stock
/// </summary>
public class InventoryOutOfStockEvent : DomainEvent<InventoryOutOfStockPayload>
{
    public override string EventType => "InventoryOutOfStock";
    public override string AggregateType => "Inventory";
}

/// <summary>
/// Payload for InventoryOutOfStock event
/// </summary>
public class InventoryOutOfStockPayload
{
    public required string ProductId { get; init; }
    public required string Sku { get; init; }
    public required DateTime OutOfStockAt { get; init; }
    public required int LastQuantity { get; init; }
    public required string LastOrderId { get; init; }
    public string? Location { get; init; }
    public bool NotificationSent { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when inventory is restocked
/// </summary>
public class InventoryRestockedEvent : DomainEvent<InventoryRestockedPayload>
{
    public override string EventType => "InventoryRestocked";
    public override string AggregateType => "Inventory";
}

/// <summary>
/// Payload for InventoryRestocked event
/// </summary>
public class InventoryRestockedPayload
{
    public required string ProductId { get; init; }
    public required string Sku { get; init; }
    public required int QuantityAdded { get; init; }
    public required int NewQuantity { get; init; }
    public required DateTime RestockedAt { get; init; }
    public required string RestockedBy { get; init; }
    public string? SupplierId { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? Location { get; init; }
    public string? BatchNumber { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}
