using FluentValidation;
using OrderAuditTrail.Shared.Events;
using OrderAuditTrail.Shared.Events.Orders;
using OrderAuditTrail.Shared.Events.Payments;
using OrderAuditTrail.Shared.Events.Inventory;
using OrderAuditTrail.Shared.Events.Shipping;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.EventIngestor.Validators;

public class EventValidator : AbstractValidator<IEvent>
{
    public EventValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("EventId is required");

        RuleFor(x => x.EventType)
            .NotEmpty()
            .WithMessage("EventType is required")
            .MaximumLength(100)
            .WithMessage("EventType must not exceed 100 characters");

        RuleFor(x => x.AggregateId)
            .NotEmpty()
            .WithMessage("AggregateId is required")
            .MaximumLength(100)
            .WithMessage("AggregateId must not exceed 100 characters");

        RuleFor(x => x.AggregateType)
            .NotEmpty()
            .WithMessage("AggregateType is required")
            .MaximumLength(100)
            .WithMessage("AggregateType must not exceed 100 characters");

        RuleFor(x => x.Version)
            .GreaterThan(0)
            .WithMessage("Version must be greater than 0");

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage("Timestamp is required")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Timestamp cannot be more than 5 minutes in the future");

        RuleFor(x => x.Metadata.Source)
            .NotEmpty()
            .WithMessage("Source is required")
            .MaximumLength(200)
            .WithMessage("Source must not exceed 200 characters");

        RuleFor(x => x.Metadata.CorrelationId)
            .MaximumLength(100)
            .WithMessage("CorrelationId must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Metadata.CorrelationId));

        RuleFor(x => x.Metadata.CausationId)
            .MaximumLength(100)
            .WithMessage("CausationId must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Metadata.CausationId));

        RuleFor(x => x.Metadata.UserId)
            .MaximumLength(100)
            .WithMessage("UserId must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Metadata.UserId));
    }
}

// Order Event Validators
public class OrderCreatedEventValidator : AbstractValidator<OrderCreatedEvent>
{
    public OrderCreatedEventValidator()
    {
        Include(new EventValidator());

        RuleFor(x => x.Payload.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required");

        RuleFor(x => x.Payload.TotalAmount)
            .GreaterThan(0)
            .WithMessage("TotalAmount must be greater than 0");

        RuleFor(x => x.Payload.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be exactly 3 characters");

        RuleFor(x => x.Payload.Items)
            .NotEmpty()
            .WithMessage("Order must have at least one item");

        RuleForEach(x => x.Payload.Items)
            .SetValidator(new OrderItemValidator());
    }
}

public class OrderItemValidator : AbstractValidator<OrderItemPayload>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("UnitPrice must be greater than 0");
    }
}

public class OrderUpdatedEventValidator : AbstractValidator<OrderUpdatedEvent>
{
    public OrderUpdatedEventValidator()
    {
        Include(new EventValidator());

        RuleFor(x => x.Payload.Changes)
            .NotEmpty()
            .WithMessage("Changes are required for order update");
    }
}

public class OrderCancelledEventValidator : AbstractValidator<OrderCancelledEvent>
{
    public OrderCancelledEventValidator()
    {
        Include(new EventValidator());

        RuleFor(x => x.Payload.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required");
    }
}

public class OrderCompletedEventValidator : AbstractValidator<OrderCompletedEvent>
{
    public OrderCompletedEventValidator()
    {
        Include(new EventValidator());

        RuleFor(x => x.Payload.CompletedAt)
            .NotEmpty()
            .WithMessage("CompletedAt is required");
    }
}

// Payment Event Validators
public class PaymentInitiatedEventValidator : AbstractValidator<OrderAuditTrail.Shared.Events.Payments.PaymentInitiatedEvent>
{
    public PaymentInitiatedEventValidator()
    {
        Include(new EventValidator());

        RuleFor(x => x.Payload.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");

        RuleFor(x => x.Payload.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Payload.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be exactly 3 characters");

        RuleFor(x => x.Payload.PaymentMethod)
            .NotEmpty()
            .WithMessage("PaymentMethod is required");
    }
}

public class PaymentCompletedEventValidator : AbstractValidator<OrderAuditTrail.Shared.Events.Payments.PaymentCompletedEvent>
{
    public PaymentCompletedEventValidator()
    {
        Include(new EventValidator());

        RuleFor(x => x.Payload.TransactionId)
            .NotEmpty()
            .WithMessage("TransactionId is required");

        RuleFor(x => x.Payload.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");
    }
}

// Inventory Event Validators
public class InventoryReservedEventValidator : AbstractValidator<InventoryReservedEvent>
{
    public InventoryReservedEventValidator()
    {
        Include(new EventValidator());

        RuleFor(x => x.Payload.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required");

        RuleFor(x => x.Payload.QuantityReserved)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.Payload.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");
    }
}

// Shipping Event Validators
public class ShipmentCreatedEventValidator : AbstractValidator<ShipmentCreatedEvent>
{
    public ShipmentCreatedEventValidator()
    {
        Include(new EventValidator());

        RuleFor(x => x.Payload.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");

        RuleFor(x => x.Payload.TrackingNumber)
            .NotEmpty()
            .When(x => x.Payload.TrackingNumber != null)
            .WithMessage("TrackingNumber cannot be empty when provided");

        RuleFor(x => x.Payload.Carrier)
            .NotEmpty()
            .WithMessage("Carrier is required");

        RuleFor(x => x.Payload.ShippingAddress)
            .NotNull()
            .WithMessage("ShippingAddress is required");
    }
}
