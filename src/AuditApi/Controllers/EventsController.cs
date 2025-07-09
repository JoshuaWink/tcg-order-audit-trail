using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderAuditTrail.AuditApi.Services;
using OrderAuditTrail.AuditApi.Models;
using System.ComponentModel.DataAnnotations;

namespace OrderAuditTrail.AuditApi.Controllers;

/// <summary>
/// Events API - Query and retrieve audit trail events
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventQueryService _eventQueryService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventQueryService eventQueryService, ILogger<EventsController> logger)
    {
        _eventQueryService = eventQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Get events with filtering and pagination
    /// </summary>
    /// <param name="request">Query parameters for filtering events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of events</returns>
    [HttpGet]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<EventDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EventDto>>>> GetEvents(
        [FromQuery] EventQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Querying events with request: {@Request}", request);

            var result = await _eventQueryService.GetEventsAsync(request, cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<EventDto>>
            {
                Success = true,
                Data = result,
                Message = "Events retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve events",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get a specific event by ID
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event details</returns>
    [HttpGet("{eventId:guid}")]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<EventDto>>> GetEvent(
        [Required] Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting event by ID: {EventId}", eventId);

            var eventDto = await _eventQueryService.GetEventByIdAsync(eventId, cancellationToken);

            if (eventDto == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Event with ID {eventId} not found"
                });
            }

            return Ok(new ApiResponse<EventDto>
            {
                Success = true,
                Data = eventDto,
                Message = "Event retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event {EventId}", eventId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve event",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all events for a specific aggregate
    /// </summary>
    /// <param name="aggregateId">Aggregate ID</param>
    /// <param name="aggregateType">Aggregate type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of events for the aggregate</returns>
    [HttpGet("aggregate/{aggregateId}")]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<List<EventDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<EventDto>>>> GetEventsByAggregate(
        [Required] string aggregateId,
        [FromQuery, Required] string aggregateType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting events for aggregate: {AggregateId} ({AggregateType})", 
                aggregateId, aggregateType);

            var events = await _eventQueryService.GetEventsByAggregateAsync(aggregateId, aggregateType, cancellationToken);

            return Ok(new ApiResponse<List<EventDto>>
            {
                Success = true,
                Data = events,
                Message = $"Retrieved {events.Count} events for aggregate {aggregateId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for aggregate {AggregateId}", aggregateId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve events for aggregate",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all events with a specific correlation ID
    /// </summary>
    /// <param name="correlationId">Correlation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of correlated events</returns>
    [HttpGet("correlation/{correlationId}")]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<List<EventDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<EventDto>>>> GetEventsByCorrelation(
        [Required] string correlationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting events by correlation ID: {CorrelationId}", correlationId);

            var events = await _eventQueryService.GetEventsByCorrelationIdAsync(correlationId, cancellationToken);

            return Ok(new ApiResponse<List<EventDto>>
            {
                Success = true,
                Data = events,
                Message = $"Retrieved {events.Count} events for correlation ID {correlationId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events by correlation ID {CorrelationId}", correlationId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve events by correlation ID",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get event statistics and metrics
    /// </summary>
    /// <param name="fromDate">Start date for statistics (optional)</param>
    /// <param name="toDate">End date for statistics (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<EventStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<EventStatisticsDto>>> GetEventStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting event statistics from {FromDate} to {ToDate}", fromDate, toDate);

            var statistics = await _eventQueryService.GetEventStatisticsAsync(fromDate, toDate, cancellationToken);

            return Ok(new ApiResponse<EventStatisticsDto>
            {
                Success = true,
                Data = statistics,
                Message = "Event statistics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event statistics");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve event statistics",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
