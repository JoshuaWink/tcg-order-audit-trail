using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.AuditApi.Models;
using OrderAuditTrail.Shared.Data;
using OrderAuditTrail.Shared.Models;
using FluentAssertions;
using AutoFixture;

namespace OrderAuditTrail.AuditApi.Tests.Integration;

public class EventsEndpointTests : IClassFixture<AuditApiTestFixture>
{
    private readonly AuditApiTestFixture _fixture;
    private readonly Fixture _autoFixture;
    private readonly HttpClient _client;

    public EventsEndpointTests(AuditApiTestFixture fixture)
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
    public async Task GetEvents_WithValidQuery_ReturnsOkWithEvents()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var events = _autoFixture.CreateMany<OrderEvent>(5).ToList();
        events.ForEach(e => e.CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10)));
        
        context.Events.AddRange(events);
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
        var response = await _client.GetAsync($"/api/events{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EventQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Events.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetEvents_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var unauthenticatedClient = _fixture.Factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEvents_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Arrange
        var queryString = "?page=1&pageSize=0";

        // Act
        var response = await _client.GetAsync($"/api/events{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEventById_WithValidId_ReturnsOkWithEvent()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetEvent = _autoFixture.Create<OrderEvent>();
        
        context.Events.Add(targetEvent);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/events/{targetEvent.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OrderEvent>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Id.Should().Be(targetEvent.Id);
        result.OrderId.Should().Be(targetEvent.OrderId);
        result.EventType.Should().Be(targetEvent.EventType);
    }

    [Fact]
    public async Task GetEventById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/events/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEventsByOrderId_WithValidOrderId_ReturnsOkWithEvents()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetOrderId = "ORDER-123";
        var events = _autoFixture.CreateMany<OrderEvent>(3).ToList();
        events.ForEach(e => e.OrderId = targetOrderId);
        
        context.Events.AddRange(events);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/events/order/{targetOrderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<OrderEvent>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
        result.Should().OnlyContain(e => e.OrderId == targetOrderId);
    }

    [Fact]
    public async Task GetEventsByOrderId_WithEmptyOrderId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/events/order/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Route not found
    }

    [Fact]
    public async Task GetEventsByEventType_WithValidEventType_ReturnsOkWithEvents()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetEventType = "OrderCreated";
        var events = _autoFixture.CreateMany<OrderEvent>(4).ToList();
        events.ForEach(e => e.EventType = targetEventType);
        
        context.Events.AddRange(events);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/events/type/{targetEventType}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<OrderEvent>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Should().HaveCount(4);
        result.Should().OnlyContain(e => e.EventType == targetEventType);
    }

    [Fact]
    public async Task GetEventsByEventType_WithNonExistentEventType_ReturnsOkWithEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/events/type/NonExistentEventType");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<OrderEvent>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEvents_WithOrderIdFilter_ReturnsFilteredEvents()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetOrderId = "ORDER-456";
        var targetEvents = _autoFixture.CreateMany<OrderEvent>(3).ToList();
        targetEvents.ForEach(e => e.OrderId = targetOrderId);
        
        var otherEvents = _autoFixture.CreateMany<OrderEvent>(5).ToList();
        
        context.Events.AddRange(targetEvents);
        context.Events.AddRange(otherEvents);
        await context.SaveChangesAsync();

        var queryString = $"?orderId={targetOrderId}";

        // Act
        var response = await _client.GetAsync($"/api/events{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EventQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Events.Should().HaveCount(3);
        result.Events.Should().OnlyContain(e => e.OrderId == targetOrderId);
    }

    [Fact]
    public async Task GetEvents_WithEventTypeFilter_ReturnsFilteredEvents()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var targetEventType = "OrderUpdated";
        var targetEvents = _autoFixture.CreateMany<OrderEvent>(2).ToList();
        targetEvents.ForEach(e => e.EventType = targetEventType);
        
        var otherEvents = _autoFixture.CreateMany<OrderEvent>(4).ToList();
        
        context.Events.AddRange(targetEvents);
        context.Events.AddRange(otherEvents);
        await context.SaveChangesAsync();

        var queryString = $"?eventType={targetEventType}";

        // Act
        var response = await _client.GetAsync($"/api/events{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EventQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Events.Should().HaveCount(2);
        result.Events.Should().OnlyContain(e => e.EventType == targetEventType);
    }

    [Fact]
    public async Task GetEvents_WithDateRangeFilter_ReturnsFilteredEvents()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow.AddDays(-5);
        
        var targetEvents = _autoFixture.CreateMany<OrderEvent>(3).ToList();
        targetEvents.ForEach(e => e.CreatedAt = startDate.AddDays(Random.Shared.Next(0, 5)));
        
        var otherEvents = _autoFixture.CreateMany<OrderEvent>(4).ToList();
        otherEvents.ForEach(e => e.CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(15, 30)));
        
        context.Events.AddRange(targetEvents);
        context.Events.AddRange(otherEvents);
        await context.SaveChangesAsync();

        var queryString = $"?startDate={startDate:yyyy-MM-ddTHH:mm:ss.fffZ}&endDate={endDate:yyyy-MM-ddTHH:mm:ss.fffZ}";

        // Act
        var response = await _client.GetAsync($"/api/events{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EventQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Events.Should().HaveCount(3);
        result.Events.Should().OnlyContain(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate);
    }

    [Fact]
    public async Task GetEvents_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await using var context = await GetDbContextAsync();
        var events = _autoFixture.CreateMany<OrderEvent>(25).ToList();
        events.ForEach(e => e.CreatedAt = DateTime.UtcNow.AddSeconds(-Random.Shared.Next(1, 1000)));
        
        context.Events.AddRange(events);
        await context.SaveChangesAsync();

        var queryString = "?page=2&pageSize=10";

        // Act
        var response = await _client.GetAsync($"/api/events{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EventQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Events.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetEvents_WithEmptyDatabase_ReturnsEmptyResult()
    {
        // Act
        var response = await _client.GetAsync("/api/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EventQueryResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result!.Events.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}
