using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Data.Repositories;
using OrderAuditTrail.AuditApi.Models;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.AuditApi.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IAuditLogRepository auditLogRepository, ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<AuditLogDto>> GetAuditLogsAsync(OrderAuditTrail.AuditApi.Models.AuditLogQueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryable = _auditLogRepository.GetQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Action))
            {
                queryable = queryable.Where(a => a.Action.Contains(request.Action));
            }

            if (!string.IsNullOrEmpty(request.EntityType))
            {
                queryable = queryable.Where(a => a.EntityType == request.EntityType);
            }

            if (!string.IsNullOrEmpty(request.EntityId))
            {
                queryable = queryable.Where(a => a.EntityId == request.EntityId);
            }

            if (!string.IsNullOrEmpty(request.UserId))
            {
                queryable = queryable.Where(a => a.UserId == request.UserId);
            }

            if (request.FromTimestamp.HasValue)
            {
                queryable = queryable.Where(a => a.Timestamp >= request.FromTimestamp.Value);
            }

            if (request.ToTimestamp.HasValue)
            {
                queryable = queryable.Where(a => a.Timestamp <= request.ToTimestamp.Value);
            }

            if (!string.IsNullOrEmpty(request.IpAddress))
            {
                queryable = queryable.Where(a => a.IpAddress == request.IpAddress);
            }

            // Apply sorting
            queryable = request.SortBy?.ToLowerInvariant() switch
            {
                "action" => request.SortDescending ? queryable.OrderByDescending(a => a.Action) : queryable.OrderBy(a => a.Action),
                "entitytype" => request.SortDescending ? queryable.OrderByDescending(a => a.EntityType) : queryable.OrderBy(a => a.EntityType),
                "entityid" => request.SortDescending ? queryable.OrderByDescending(a => a.EntityId) : queryable.OrderBy(a => a.EntityId),
                "userid" => request.SortDescending ? queryable.OrderByDescending(a => a.UserId) : queryable.OrderBy(a => a.UserId),
                _ => request.SortDescending ? queryable.OrderByDescending(a => a.Timestamp) : queryable.OrderBy(a => a.Timestamp)
            };

            var totalCount = await queryable.CountAsync(cancellationToken);

            var auditLogs = await queryable
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var auditLogDtos = auditLogs.Select(MapToDto).ToList();

            return new PaginatedResponse<AuditLogDto>
            {
                Items = auditLogDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying audit logs with request: {@Request}", request);
            throw;
        }
    }

    public async Task<AuditLogDto?> GetAuditLogByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            var (auditLogs, _) = await _auditLogRepository.GetAuditLogsAsync(
                pageSize: 1,
                pageNumber: 1,
                cancellationToken: cancellationToken);
            
            var auditLog = auditLogs.FirstOrDefault(a => a.Id == id);
            return auditLog != null ? MapToDto(auditLog) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log by ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var (auditLogs, _) = await _auditLogRepository.GetAuditLogsAsync(
                userId: userId,
                pageSize: 1000,
                pageNumber: 1,
                cancellationToken: cancellationToken);
            
            return auditLogs.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs by user ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByActionTypeAsync(string actionType, CancellationToken cancellationToken = default)
    {
        try
        {
            var (auditLogs, _) = await _auditLogRepository.GetAuditLogsAsync(
                action: actionType,
                pageSize: 1000,
                pageNumber: 1,
                cancellationToken: cancellationToken);
            
            return auditLogs.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs by action type: {ActionType}", actionType);
            throw;
        }
    }

    public async Task<AuditLogDto> CreateAuditLogAsync(AuditLogDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogEntity = new AuditLogEntity
            {
                EventId = request.EventId == Guid.Empty ? null : request.EventId,
                UserId = request.UserId,
                Action = request.Action,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                Details = request.Details,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                Timestamp = DateTime.UtcNow
            };

            var createdEntity = await _auditLogRepository.AddAsync(auditLogEntity, cancellationToken);
            return MapToDto(createdEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log");
            throw;
        }
    }

    private static AuditLogDto MapToDto(AuditLogEntity entity)
    {
        return new AuditLogDto
        {
            Id = entity.Id,
            EventId = entity.EventId,
            Action = entity.Action,
            EntityType = entity.EntityType,
            EntityId = entity.EntityId,
            UserId = entity.UserId,
            Timestamp = entity.Timestamp,
            Details = entity.Details,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent
        };
    }
}
