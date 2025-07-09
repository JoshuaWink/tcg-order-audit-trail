using FluentValidation;
using OrderAuditTrail.Shared.Events;

namespace OrderAuditTrail.EventIngestor.Services;

public interface IEventValidationService
{
    Task<ValidationResult> ValidateEventAsync(IEvent eventData, CancellationToken cancellationToken);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class EventValidationService : IEventValidationService
{
    private readonly ILogger<EventValidationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EventValidationService(
        ILogger<EventValidationService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<ValidationResult> ValidateEventAsync(IEvent eventData, CancellationToken cancellationToken)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            // Basic validation
            if (eventData == null)
            {
                result.IsValid = false;
                result.Errors.Add("Event data is null");
                return result;
            }

            // Validate core event properties
            var coreValidationErrors = ValidateCoreProperties(eventData);
            if (coreValidationErrors.Any())
            {
                result.IsValid = false;
                result.Errors.AddRange(coreValidationErrors);
            }

            // Get the appropriate validator for the event type
            var validatorType = typeof(IValidator<>).MakeGenericType(eventData.GetType());
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator != null)
            {
                var validationContext = new ValidationContext<object>(eventData);
                var validationResult = await validator.ValidateAsync(validationContext, cancellationToken);

                if (!validationResult.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
                }
            }
            else
            {
                _logger.LogWarning("No validator found for event type {EventType}", eventData.GetType().Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating event {EventType}", eventData?.GetType().Name);
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
            return result;
        }
    }

    private List<string> ValidateCoreProperties(IEvent eventData)
    {
        var errors = new List<string>();

        if (eventData.EventId == Guid.Empty)
        {
            errors.Add("EventId cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(eventData.EventType))
        {
            errors.Add("EventType cannot be null or empty");
        }

        if (eventData.Timestamp == default)
        {
            errors.Add("Timestamp cannot be default value");
        }

        if (eventData.Timestamp > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("Timestamp cannot be in the future");
        }

        if (eventData.Version <= 0)
        {
            errors.Add("Version must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(eventData.Source))
        {
            errors.Add("Source cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(eventData.AggregateId))
        {
            errors.Add("AggregateId cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(eventData.AggregateType))
        {
            errors.Add("AggregateType cannot be null or empty");
        }

        return errors;
    }
}
