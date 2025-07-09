using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderAuditTrail.AuditApi.Services;
using OrderAuditTrail.AuditApi.Models;
using System.ComponentModel.DataAnnotations;

namespace OrderAuditTrail.AuditApi.Controllers;

/// <summary>
/// Event Replay API - Start and manage event replay operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class ReplayController : ControllerBase
{
    private readonly IEventReplayService _eventReplayService;
    private readonly ILogger<ReplayController> _logger;

    public ReplayController(IEventReplayService eventReplayService, ILogger<ReplayController> logger)
    {
        _eventReplayService = eventReplayService;
        _logger = logger;
    }

    /// <summary>
    /// Start a new event replay operation
    /// </summary>
    /// <param name="request">Event replay request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Replay operation ID</returns>
    [HttpPost]
    [Authorize(Policy = "RequireWriteAccess")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<string>>> StartEventReplay(
        [FromBody] EventReplayRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting event replay for aggregate {AggregateId} ({AggregateType})", 
                request.AggregateId, request.AggregateType);

            // Set requested by from user context
            request.RequestedBy = User.Identity?.Name ?? "unknown";

            var replayId = await _eventReplayService.StartEventReplayAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetEventReplayStatus),
                new { replayId },
                new ApiResponse<string>
                {
                    Success = true,
                    Data = replayId,
                    Message = "Event replay started successfully"
                });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid replay request");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid replay request",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot start replay");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Cannot start replay",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting event replay");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to start event replay",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get the status of a specific event replay operation
    /// </summary>
    /// <param name="replayId">Replay operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Replay operation status</returns>
    [HttpGet("{replayId}")]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<EventReplayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<EventReplayDto>>> GetEventReplayStatus(
        [Required] string replayId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting replay status for: {ReplayId}", replayId);

            var replay = await _eventReplayService.GetEventReplayStatusAsync(replayId, cancellationToken);

            if (replay == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Replay with ID {replayId} not found"
                });
            }

            return Ok(new ApiResponse<EventReplayDto>
            {
                Success = true,
                Data = replay,
                Message = "Replay status retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving replay status for {ReplayId}", replayId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve replay status",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all event replay operations with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of replay operations</returns>
    [HttpGet]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<EventReplayDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EventReplayDto>>>> GetEventReplays(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting event replays - Page: {PageNumber}, Size: {PageSize}", 
                pageNumber, pageSize);

            var replays = await _eventReplayService.GetEventReplaysAsync(pageNumber, pageSize, cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<EventReplayDto>>
            {
                Success = true,
                Data = replays,
                Message = "Event replays retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event replays");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve event replays",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Cancel a running event replay operation
    /// </summary>
    /// <param name="replayId">Replay operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{replayId}")]
    [Authorize(Policy = "RequireWriteAccess")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> CancelEventReplay(
        [Required] string replayId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling replay: {ReplayId}", replayId);

            var cancelledBy = User.Identity?.Name ?? "unknown";
            var success = await _eventReplayService.CancelEventReplayAsync(replayId, cancelledBy, cancellationToken);

            if (!success)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Replay with ID {replayId} not found or cannot be cancelled"
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Event replay cancelled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling replay {ReplayId}", replayId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to cancel event replay",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
