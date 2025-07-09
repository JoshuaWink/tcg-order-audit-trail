using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderAuditTrail.AuditApi.Services;
using OrderAuditTrail.AuditApi.Models;
using System.ComponentModel.DataAnnotations;

namespace OrderAuditTrail.AuditApi.Controllers;

/// <summary>
/// Audit Logs API - Query system audit logs and access history
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IAuditLogService auditLogService, ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    /// <param name="request">Query parameters for filtering audit logs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of audit logs</returns>
    [HttpGet]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<AuditLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] AuditLogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Querying audit logs with request: {@Request}", request);

            var result = await _auditLogService.GetAuditLogsAsync(request, cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<AuditLogDto>>
            {
                Success = true,
                Data = result,
                Message = "Audit logs retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve audit logs",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get a specific audit log by ID
    /// </summary>
    /// <param name="id">Audit log ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit log details</returns>
    [HttpGet("{id:long}")]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuditLogDto>>> GetAuditLog(
        [Required] long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting audit log by ID: {Id}", id);

            var auditLog = await _auditLogService.GetAuditLogByIdAsync(id, cancellationToken);

            if (auditLog == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Audit log with ID {id} not found"
                });
            }

            return Ok(new ApiResponse<AuditLogDto>
            {
                Success = true,
                Data = auditLog,
                Message = "Audit log retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log {Id}", id);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve audit log",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all audit logs for a specific entity
    /// </summary>
    /// <param name="entityType">Entity type (e.g., 'Order', 'Payment')</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs for the entity</returns>
    [HttpGet("entity/{entityType}/{entityId}")]
    [Authorize(Policy = "RequireReadAccess")]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<AuditLogDto>>>> GetAuditLogsByEntity(
        [Required] string entityType,
        [Required] string entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting audit logs for entity: {EntityType} {EntityId}", entityType, entityId);

            var auditLogs = await _auditLogService.GetAuditLogsByEntityAsync(entityType, entityId, cancellationToken);

            return Ok(new ApiResponse<List<AuditLogDto>>
            {
                Success = true,
                Data = auditLogs,
                Message = $"Retrieved {auditLogs.Count} audit logs for {entityType} {entityId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for entity {EntityType} {EntityId}", entityType, entityId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve audit logs for entity",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
