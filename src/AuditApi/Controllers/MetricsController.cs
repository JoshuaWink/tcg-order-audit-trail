using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderAuditTrail.AuditApi.Services;
using OrderAuditTrail.AuditApi.Models;

namespace OrderAuditTrail.AuditApi.Controllers;

/// <summary>
/// Metrics API - Query system metrics and performance data
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IMetricsQueryService _metricsQueryService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(IMetricsQueryService metricsQueryService, ILogger<MetricsController> logger)
    {
        _metricsQueryService = metricsQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Get system metrics with filtering and pagination
    /// </summary>
    /// <param name="request">Metrics query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of metrics</returns>
    [HttpGet]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<MetricsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<MetricsDto>>>> GetMetrics(
        [FromQuery] MetricsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Querying metrics with request: {@Request}", request);

            var result = await _metricsQueryService.GetMetricsAsync(request, cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<MetricsDto>>
            {
                Success = true,
                Data = result,
                Message = "Metrics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve metrics",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get event-specific metrics and statistics
    /// </summary>
    /// <param name="fromDate">Start date for metrics (optional)</param>
    /// <param name="toDate">End date for metrics (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event metrics</returns>
    [HttpGet("events")]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<List<EventMetricsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<EventMetricsDto>>>> GetEventMetrics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting event metrics from {FromDate} to {ToDate}", fromDate, toDate);

            var metrics = await _metricsQueryService.GetEventMetricsAsync(fromDate, toDate, cancellationToken);

            return Ok(new ApiResponse<List<EventMetricsDto>>
            {
                Success = true,
                Data = metrics,
                Message = "Event metrics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event metrics");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve event metrics",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get overall system health and status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System health information</returns>
    [HttpGet("health")]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SystemHealthDto>>> GetSystemHealth(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting system health");

            var health = await _metricsQueryService.GetSystemHealthAsync(cancellationToken);

            return Ok(new ApiResponse<SystemHealthDto>
            {
                Success = true,
                Data = health,
                Message = "System health retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system health");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve system health",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
