using OrderAuditTrail.AuditApi.Models;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.AuditApi.Services;

public interface IEventQueryService
{
    Task<PaginatedResponse<EventDto>> GetEventsAsync(OrderAuditTrail.AuditApi.Models.EventQueryRequest request, CancellationToken cancellationToken = default);
    Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventDto>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventDto>> GetEventsByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);
}

public interface IEventReplayService
{
    Task<EventReplayDto> StartReplayAsync(EventReplayRequest request, CancellationToken cancellationToken = default);
    Task<EventReplayDto?> GetReplayStatusAsync(string replayId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CancelReplayAsync(string replayId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventReplayDto>> GetReplayHistoryAsync(CancellationToken cancellationToken = default);
}

public interface IMetricsQueryService
{
    Task<SystemHealthDto> GetSystemMetricsAsync(CancellationToken cancellationToken = default);
    Task<EventStatisticsDto> GetEventMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<MetricsDto>> GetMetricsAsync(MetricsQueryRequest request, CancellationToken cancellationToken = default);
}

public interface IAuditLogService
{
    Task<PaginatedResponse<AuditLogDto>> GetAuditLogsAsync(OrderAuditTrail.AuditApi.Models.AuditLogQueryRequest request, CancellationToken cancellationToken = default);
    Task<AuditLogDto?> GetAuditLogByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByActionTypeAsync(string actionType, CancellationToken cancellationToken = default);
    Task<AuditLogDto> CreateAuditLogAsync(AuditLogDto request, CancellationToken cancellationToken = default);
}
