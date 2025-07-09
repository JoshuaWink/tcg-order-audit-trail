using OrderAuditTrail.Shared.Events;

namespace OrderAuditTrail.Shared.Events.Payments;

/// <summary>
/// Event fired when a payment is initiated
/// </summary>
public class PaymentInitiatedEvent : DomainEvent<PaymentInitiatedPayload>
{
    public override string EventType => "PaymentInitiated";
    public override string AggregateType => "Payment";
}

/// <summary>
/// Payload for PaymentInitiated event
/// </summary>
public class PaymentInitiatedPayload
{
    public required string PaymentId { get; init; }
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PaymentMethod { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a payment is completed
/// </summary>
public class PaymentCompletedEvent : DomainEvent<PaymentCompletedPayload>
{
    public override string EventType => "PaymentCompleted";
    public override string AggregateType => "Payment";
}

/// <summary>
/// Payload for PaymentCompleted event
/// </summary>
public class PaymentCompletedPayload
{
    public required string PaymentId { get; init; }
    public required string OrderId { get; init; }
    public required string TransactionId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Status { get; init; }
    public required DateTime CompletedAt { get; init; }
    public string? ProcessorResponse { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a payment is processed successfully
/// </summary>
public class PaymentProcessedEvent : DomainEvent<PaymentProcessedPayload>
{
    public override string EventType => "PaymentProcessed";
    public override string AggregateType => "Payment";
}

/// <summary>
/// Payload for PaymentProcessed event
/// </summary>
public class PaymentProcessedPayload
{
    public required string PaymentId { get; init; }
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PaymentMethod { get; init; }
    public required string TransactionId { get; init; }
    public required string Status { get; init; }
    public required DateTime ProcessedAt { get; init; }
    public string? ProcessorResponse { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a payment processing fails
/// </summary>
public class PaymentFailedEvent : DomainEvent<PaymentFailedPayload>
{
    public override string EventType => "PaymentFailed";
    public override string AggregateType => "Payment";
}

/// <summary>
/// Payload for PaymentFailed event
/// </summary>
public class PaymentFailedPayload
{
    public required string PaymentId { get; init; }
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PaymentMethod { get; init; }
    public required string ErrorCode { get; init; }
    public required string ErrorMessage { get; init; }
    public required DateTime FailedAt { get; init; }
    public string? ProcessorResponse { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a payment is refunded
/// </summary>
public class PaymentRefundedEvent : DomainEvent<PaymentRefundedPayload>
{
    public override string EventType => "PaymentRefunded";
    public override string AggregateType => "Payment";
}

/// <summary>
/// Payload for PaymentRefunded event
/// </summary>
public class PaymentRefundedPayload
{
    public required string PaymentId { get; init; }
    public required string RefundId { get; init; }
    public required string OrderId { get; init; }
    public required decimal RefundAmount { get; init; }
    public required string Currency { get; init; }
    public required string Reason { get; init; }
    public required string RefundedBy { get; init; }
    public required DateTime RefundedAt { get; init; }
    public string? TransactionId { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a payment refund fails
/// </summary>
public class PaymentRefundFailedEvent : DomainEvent<PaymentRefundFailedPayload>
{
    public override string EventType => "PaymentRefundFailed";
    public override string AggregateType => "Payment";
}

/// <summary>
/// Payload for PaymentRefundFailed event
/// </summary>
public class PaymentRefundFailedPayload
{
    public required string PaymentId { get; init; }
    public required string RefundId { get; init; }
    public required string OrderId { get; init; }
    public required decimal RefundAmount { get; init; }
    public required string Currency { get; init; }
    public required string Reason { get; init; }
    public required string ErrorCode { get; init; }
    public required string ErrorMessage { get; init; }
    public required DateTime FailedAt { get; init; }
    public string? ProcessorResponse { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Event fired when a payment is disputed
/// </summary>
public class PaymentDisputedEvent : DomainEvent<PaymentDisputedPayload>
{
    public override string EventType => "PaymentDisputed";
    public override string AggregateType => "Payment";
}

/// <summary>
/// Payload for PaymentDisputed event
/// </summary>
public class PaymentDisputedPayload
{
    public required string PaymentId { get; init; }
    public required string OrderId { get; init; }
    public required string DisputeId { get; init; }
    public required decimal DisputeAmount { get; init; }
    public required string Currency { get; init; }
    public required string Reason { get; init; }
    public required string Status { get; init; }
    public required DateTime DisputedAt { get; init; }
    public string? Evidence { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}
