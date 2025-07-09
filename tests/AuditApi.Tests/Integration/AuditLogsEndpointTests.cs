using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.AuditApi.Models;
using OrderAuditTrail.Shared.Data;
using FluentAssertions;
using AutoFixture;

namespace OrderAuditTrail.AuditApi.Tests.Integration;

public class AuditLogsEndpointTests : IClassFixture<AuditApiTestFixture>
{
    private readonly AuditApiTestFixture _fixture;
    private readonly Fixture _autoFixture;
    private readonly HttpClient _client;

    public AuditLogsEndpointTests(AuditApiTestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
        _client = _fixture.Factory.CreateAuthenticatedClient();
        
        // Configure AutoFixture
        _autoFixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _autoFixture.Behaviors.Remove(b));
        _autoFixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    private async Task<AuditDbContext> GetDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(_fixture.Factory.PostgresConnectionString)
            .Options;
        
        var context = new AuditDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task GetAuditLogs_WithValidQuery_ReturnsOkWithLogs()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var auditLogs = _autoFixture.CreateMany<AuditLog>(5).ToList();
        auditLogs.ForEach(log => log.CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10)));
        
        context.AuditLogs.AddRange(auditLogs);
        await context.SaveChangesAsync();

        var query = new
        {
            page = 1,
            pageSize = 10,
            startDate = DateTime.UtcNow.AddDays(-15).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            endDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };

        var queryString = $"?page={query.page}&pageSize={query.pageSize}&startDate={query.startDate}&endDate={query.endDate}";

        // Act
        var response = await _client.GetAsync($"/api/audit-logs{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLogQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.AuditLogs.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAuditLogs_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var unauthenticatedClient = _fixture.Factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditLogs_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Arrange
        var queryString = "?page=1&pageSize=0";

        // Act
        var response = await _client.GetAsync($"/api/audit-logs{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAuditLogById_WithValidId_ReturnsOkWithLog()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetLog = _autoFixture.Create<AuditLog>();
        
        context.AuditLogs.Add(targetLog);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/audit-logs/{targetLog.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLog>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Id.Should().Be(targetLog.Id);
        result.UserId.Should().Be(targetLog.UserId);
        result.ActionType.Should().Be(targetLog.ActionType);
    }

    [Fact]
    public async Task GetAuditLogById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/audit-logs/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAuditLogsByUserId_WithValidUserId_ReturnsOkWithLogs()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetUserId = "user123";
        var auditLogs = _autoFixture.CreateMany<AuditLog>(3).ToList();
        auditLogs.ForEach(log => log.UserId = targetUserId);
        
        context.AuditLogs.AddRange(auditLogs);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/audit-logs/user/{targetUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AuditLog>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
        result.Should().OnlyContain(log => log.UserId == targetUserId);
    }

    [Fact]
    public async Task GetAuditLogsByActionType_WithValidActionType_ReturnsOkWithLogs()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetActionType = "Query";
        var auditLogs = _autoFixture.CreateMany<AuditLog>(4).ToList();
        auditLogs.ForEach(log => log.ActionType = targetActionType);
        
        context.AuditLogs.AddRange(auditLogs);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/audit-logs/action/{targetActionType}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AuditLog>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Should().HaveCount(4);
        result.Should().OnlyContain(log => log.ActionType == targetActionType);
    }

    [Fact]
    public async Task CreateAuditLog_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateAuditLogRequest
        {
            ActionType = "Query",
            ResourceId = "EVENT-123",
            ResourceType = "Event",
            Description = "User queried event details",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0..."
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/audit-logs", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLog>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.ActionType.Should().Be(request.ActionType);
        result.ResourceId.Should().Be(request.ResourceId);
        result.ResourceType.Should().Be(request.ResourceType);
        result.Description.Should().Be(request.Description);
        result.IpAddress.Should().Be(request.IpAddress);
        result.UserAgent.Should().Be(request.UserAgent);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify it was saved to the database
        await using var context = await GetDbContextAsync();
        var savedLog = await context.AuditLogs.FindAsync(result.Id);
        savedLog.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAuditLog_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateAuditLogRequest
        {
            ActionType = "", // Invalid - empty action type
            ResourceId = "EVENT-123",
            ResourceType = "Event"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/audit-logs", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAuditLogsSummary_ReturnsOkWithSummary()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var now = DateTime.UtcNow;
        var auditLogs = new List<AuditLog>();

        // Create logs for different time periods and users
        for (int i = 0; i < 5; i++)
        {
            auditLogs.Add(_autoFixture.Build<AuditLog>()
                .With(l => l.CreatedAt, now.AddHours(-i))
                .With(l => l.UserId, "user1")
                .With(l => l.ActionType, "Query")
                .Create());
        }

        for (int i = 0; i < 3; i++)
        {
            auditLogs.Add(_autoFixture.Build<AuditLog>()
                .With(l => l.CreatedAt, now.AddDays(-i - 1))
                .With(l => l.UserId, "user2")
                .With(l => l.ActionType, "Replay")
                .Create());
        }

        context.AuditLogs.AddRange(auditLogs);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/audit-logs/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLogsSummary>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.TotalLogs.Should().Be(8);
        result.LogsLast24Hours.Should().Be(5);
        result.LogsLast7Days.Should().Be(8);
        result.TopUsers.Should().HaveCount(2);
        result.TopActionTypes.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAuditLogs_WithUserIdFilter_ReturnsFilteredLogs()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetUserId = "user456";
        var targetLogs = _autoFixture.CreateMany<AuditLog>(3).ToList();
        targetLogs.ForEach(log => log.UserId = targetUserId);
        
        var otherLogs = _autoFixture.CreateMany<AuditLog>(5).ToList();
        
        context.AuditLogs.AddRange(targetLogs);
        context.AuditLogs.AddRange(otherLogs);
        await context.SaveChangesAsync();

        var queryString = $"?userId={targetUserId}";

        // Act
        var response = await _client.GetAsync($"/api/audit-logs{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLogQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.AuditLogs.Should().HaveCount(3);
        result.AuditLogs.Should().OnlyContain(log => log.UserId == targetUserId);
    }

    [Fact]
    public async Task GetAuditLogs_WithActionTypeFilter_ReturnsFilteredLogs()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetActionType = "Export";
        var targetLogs = _autoFixture.CreateMany<AuditLog>(2).ToList();
        targetLogs.ForEach(log => log.ActionType = targetActionType);
        
        var otherLogs = _autoFixture.CreateMany<AuditLog>(4).ToList();
        
        context.AuditLogs.AddRange(targetLogs);
        context.AuditLogs.AddRange(otherLogs);
        await context.SaveChangesAsync();

        var queryString = $"?actionType={targetActionType}";

        // Act
        var response = await _client.GetAsync($"/api/audit-logs{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLogQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.AuditLogs.Should().HaveCount(2);
        result.AuditLogs.Should().OnlyContain(log => log.ActionType == targetActionType);
    }

    [Fact]
    public async Task GetAuditLogs_WithDateRangeFilter_ReturnsFilteredLogs()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow.AddDays(-5);
        
        var targetLogs = _autoFixture.CreateMany<AuditLog>(3).ToList();
        targetLogs.ForEach(log => log.CreatedAt = startDate.AddDays(Random.Shared.Next(0, 5)));
        
        var otherLogs = _autoFixture.CreateMany<AuditLog>(4).ToList();
        otherLogs.ForEach(log => log.CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(15, 30)));
        
        context.AuditLogs.AddRange(targetLogs);
        context.AuditLogs.AddRange(otherLogs);
        await context.SaveChangesAsync();

        var queryString = $"?startDate={startDate:yyyy-MM-ddTHH:mm:ss.fffZ}&endDate={endDate:yyyy-MM-ddTHH:mm:ss.fffZ}";

        // Act
        var response = await _client.GetAsync($"/api/audit-logs{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLogQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.AuditLogs.Should().HaveCount(3);
        result.AuditLogs.Should().OnlyContain(log => log.CreatedAt >= startDate && log.CreatedAt <= endDate);
    }

    [Fact]
    public async Task GetAuditLogs_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var auditLogs = _autoFixture.CreateMany<AuditLog>(25).ToList();
        auditLogs.ForEach(log => log.CreatedAt = DateTime.UtcNow.AddSeconds(-Random.Shared.Next(1, 1000)));
        
        context.AuditLogs.AddRange(auditLogs);
        await context.SaveChangesAsync();

        var queryString = "?page=2&pageSize=10";

        // Act
        var response = await _client.GetAsync($"/api/audit-logs{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLogQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.AuditLogs.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetAuditLogs_WithEmptyDatabase_ReturnsEmptyResult()
    {
        // Act
        var response = await _client.GetAsync("/api/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuditLogQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.AuditLogs.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}
