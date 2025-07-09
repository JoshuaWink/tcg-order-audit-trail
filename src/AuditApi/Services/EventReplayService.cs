using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Data.Repositories;
using OrderAuditTrail.AuditApi.Models;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.AuditApi.Services;

public class EventReplayService : IEventReplayService
{
    private readonly IEventReplayRepository _replayRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventReplayService> _logger;

    public EventReplayService(
        IEventReplayRepository replayRepository,
        IEventRepository eventRepository,
        ILogger<EventReplayService> logger)
    {
        _replayRepository = replayRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<EventReplayDto> StartReplayAsync(EventReplayRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var replayId = Guid.NewGuid();

            // Validate date range
            if (request.EndDate <= request.StartDate)
            {
                throw new ArgumentException("EndDate must be after StartDate");
            }

            // Check if there are events to replay
            var eventCount = await _eventRepository.GetQueryable()
                .Where(e => e.AggregateId == request.OrderId &&
                           e.Timestamp >= request.StartDate &&
                           e.Timestamp <= request.EndDate)
                .CountAsync(cancellationToken);

            if (eventCount == 0)
            {
                throw new InvalidOperationException("No events found for the specified criteria");
            }

            // Create replay record
            var replay = new EventReplayEntity
            {
                ReplayId = replayId.ToString(),
                AggregateId = request.OrderId,
                AggregateType = "Order",
                FromTimestamp = request.StartDate,
                ToTimestamp = request.EndDate,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                EventsReplayed = 0,
                RequestedBy = request.RequestedBy ?? "system"
            };

            await _replayRepository.AddAsync(replay, cancellationToken);

            _logger.LogInformation("Event replay started: {ReplayId} for order {OrderId}", 
                replayId, request.OrderId);

            // Start background replay process
            _ = Task.Run(async () => await ProcessEventReplayAsync(replayId.ToString(), new EventReplayRequest
            {
                AggregateId = request.OrderId,
                AggregateType = "Order",
                FromTimestamp = request.StartDate,
                ToTimestamp = request.EndDate,
                EventType = request.EventType,
                TargetTopic = request.TargetTopic,
                RequestedBy = request.RequestedBy
            }), cancellationToken);

            return MapToDto(replay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting event replay for order {OrderId}", request.OrderId);
            throw;
        }
    }

    public async Task<EventReplayDto?> GetReplayStatusAsync(string replayId, CancellationToken cancellationToken = default)
    {
        try
        {
            var replay = await _replayRepository.GetByReplayIdAsync(replayId, cancellationToken);
            return replay != null ? MapToDto(replay) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event replay status: {ReplayId}", replayId);
            throw;
        }
    }

    public async Task<ApiResponse<bool>> CancelReplayAsync(string replayId, CancellationToken cancellationToken = default)
    {
        try
        {
            var replay = await _replayRepository.GetByReplayIdAsync(replayId.ToString(), cancellationToken);
            if (replay == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "Replay not found"
                };
            }

            if (replay.Status == "COMPLETED" || replay.Status == "CANCELLED")
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Replay is already {replay.Status.ToLower()}"
                };
            }

            replay.Status = "CANCELLED";
            replay.CompletedAt = DateTime.UtcNow;
            replay.ErrorDetails = "Cancelled by user";

            await _replayRepository.UpdateAsync(replay, cancellationToken);

            _logger.LogInformation("Event replay cancelled: {ReplayId}", replayId);

            return new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Replay cancelled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event replay: {ReplayId}", replayId);
            throw;
        }
    }

    public async Task<IEnumerable<ReplayHistoryItem>> GetReplayHistoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var replays = await _replayRepository.GetQueryable()
                .OrderByDescending(r => r.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            return replays.Select(r => new ReplayHistoryItem
            {
                ReplayId = Guid.Parse(r.ReplayId),
                OrderId = r.AggregateId,
                Status = r.Status,
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,
                EventsReplayed = r.EventsReplayed,
                RequestedBy = r.RequestedBy
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting replay history");
            throw;
        }
    }

    public async Task<EventReplayDto?> GetEventReplayStatusAsync(string replayId, CancellationToken cancellationToken = default)
    {
        try
        {
            var replay = await _replayRepository.GetByReplayIdAsync(replayId, cancellationToken);
            return replay != null ? MapToDto(replay) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event replay status: {ReplayId}", replayId);
            throw;
        }
    }

    public async Task<PaginatedResponse<EventReplayDto>> GetEventReplaysAsync(int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryable = _replayRepository.GetQueryable().OrderByDescending(r => r.CreatedAt);
            
            var totalCount = await queryable.CountAsync(cancellationToken);
            
            var replays = await queryable
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var replayDtos = replays.Select(MapToDto).ToList();

            return new PaginatedResponse<EventReplayDto>
            {
                Items = replayDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event replays");
            throw;
        }
    }

    public async Task<bool> CancelEventReplayAsync(string replayId, string cancelledBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var replay = await _replayRepository.GetByReplayIdAsync(replayId, cancellationToken);
            if (replay == null)
            {
                return false;
            }

            if (replay.Status == "COMPLETED" || replay.Status == "CANCELLED")
            {
                return false;
            }

            replay.Status = "CANCELLED";
            replay.CompletedAt = DateTime.UtcNow;
            replay.ErrorDetails = $"Cancelled by {cancelledBy}";

            await _replayRepository.UpdateAsync(replay, cancellationToken);

            _logger.LogInformation("Event replay cancelled: {ReplayId} by {CancelledBy}", replayId, cancelledBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event replay: {ReplayId}", replayId);
            throw;
        }
    }

    private async Task ProcessEventReplayAsync(string replayId, EventReplayRequest request)
    {
        try
        {
            var replay = await _replayRepository.GetByReplayIdAsync(replayId);
            if (replay == null)
            {
                _logger.LogError("Replay not found: {ReplayId}", replayId);
                return;
            }

            // Update status to RUNNING
            replay.Status = "RUNNING";
            replay.StartedAt = DateTime.UtcNow;
            await _replayRepository.UpdateAsync(replay);

            // Get events to replay
            var events = await _eventRepository.GetByAggregateIdAsync(
                request.AggregateId, 
                request.AggregateType);

            var eventsToReplay = events
                .Where(e => e.Timestamp >= request.FromTimestamp && e.Timestamp <= request.ToTimestamp)
                .OrderBy(e => e.Timestamp)
                .ThenBy(e => e.Version)
                .ToList();

            if (!string.IsNullOrEmpty(request.EventType))
            {
                eventsToReplay = eventsToReplay.Where(e => e.EventType == request.EventType).ToList();
            }

            int replayedCount = 0;

            foreach (var eventEntity in eventsToReplay)
            {
                try
                {
                    // Here you would publish the event to the target topic
                    // For now, we'll just simulate the replay
                    await SimulateEventReplayAsync(eventEntity, request.TargetTopic);
                    
                    replayedCount++;
                    
                    // Update progress every 10 events
                    if (replayedCount % 10 == 0)
                    {
                        replay.EventsReplayed = replayedCount;
                        await _replayRepository.UpdateAsync(replay);
                    }

                    // Small delay to prevent overwhelming the system
                    await Task.Delay(10);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error replaying event {EventId} for replay {ReplayId}", 
                        eventEntity.EventId, replayId);
                }
            }

            // Mark as completed
            replay.Status = "COMPLETED";
            replay.CompletedAt = DateTime.UtcNow;
            replay.EventsReplayed = replayedCount;
            await _replayRepository.UpdateAsync(replay);

            _logger.LogInformation("Event replay completed: {ReplayId}, replayed {Count} events", 
                replayId, replayedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event replay: {ReplayId}", replayId);
            
            // Mark as failed
            var replay = await _replayRepository.GetByReplayIdAsync(replayId);
            if (replay != null)
            {
                replay.Status = "FAILED";
                replay.CompletedAt = DateTime.UtcNow;
                replay.ErrorDetails = ex.Message;
                await _replayRepository.UpdateAsync(replay);
            }
        }
    }

    private async Task SimulateEventReplayAsync(EventEntity eventEntity, string? targetTopic)
    {
        // In a real implementation, you would:
        // 1. Publish the event to Kafka topic
        // 2. Handle any publishing errors
        // 3. Update metrics

        _logger.LogDebug("Replaying event {EventId} to topic {Topic}", 
            eventEntity.EventId, targetTopic ?? "default");

        // Simulate async operation
        await Task.Delay(1);
    }

    private static EventReplayDto MapToDto(EventReplayEntity entity)
    {
        return new EventReplayDto
        {
            Id = entity.Id,
            ReplayId = entity.ReplayId,
            AggregateId = entity.AggregateId,
            AggregateType = entity.AggregateType,
            FromTimestamp = entity.FromTimestamp,
            ToTimestamp = entity.ToTimestamp,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            ErrorDetails = entity.ErrorDetails,
            EventsReplayed = entity.EventsReplayed,
            RequestedBy = entity.RequestedBy
        };
    }
}
