using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderAuditTrail.AuditApi.Controllers;
using OrderAuditTrail.AuditApi.Models;
using OrderAuditTrail.AuditApi.Services;
using FluentAssertions;
using AutoFixture;
using AutoFixture.Xunit2;
using Microsoft.AspNetCore.Http;

namespace OrderAuditTrail.AuditApi.Tests.Controllers;

public class AuditLogsControllerTests : IClassFixture<AuditApiTestFixture>
{
    private readonly AuditApiTestFixture _fixture;
    private readonly Fixture _autoFixture;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger<AuditLogsController>> _mockLogger;
    private readonly AuditLogsController _controller;

    public AuditLogsControllerTests(AuditApiTestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<AuditLogsController>>();
        
        _controller = new AuditLogsController(_mockAuditLogService.Object, _mockLogger.Object);
        
        // Setup controller context for testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAuditLogs_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var request = new AuditLogQueryRequest
        {
            Page = 1,
            PageSize = 10,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            UserId = "user123",
            ActionType = "Query"
        };

        var auditLogs = _autoFixture.CreateMany<AuditLog>(5).ToList();
        var expectedResponse = new AuditLogQueryResponse
        {
            AuditLogs = auditLogs,
            TotalCount = auditLogs.Count,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = 1
        };

        _mockAuditLogService
            .Setup(s => s.GetAuditLogsAsync(It.IsAny<AuditLogQueryRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAuditLogs(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
        
        _mockAuditLogService.Verify(s => s.GetAuditLogsAsync(It.IsAny<AuditLogQueryRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogs_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Arrange
        var request = new AuditLogQueryRequest
        {
            Page = 1,
            PageSize = 0 // Invalid page size
        };

        // Act
        var result = await _controller.GetAuditLogs(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result as BadRequestObjectResult;
        badResult!.Value.Should().NotBeNull();
        
        _mockAuditLogService.Verify(s => s.GetAuditLogsAsync(It.IsAny<AuditLogQueryRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetAuditLogs_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var request = new AuditLogQueryRequest
        {
            Page = 1,
            PageSize = 10,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-1) // End date before start date
        };

        // Act
        var result = await _controller.GetAuditLogs(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badResult = result as BadRequestObjectResult;
        badResult!.Value.Should().NotBeNull();
        
        _mockAuditLogService.Verify(s => s.GetAuditLogsAsync(It.IsAny<AuditLogQueryRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetAuditLogs_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var request = new AuditLogQueryRequest
        {
            Page = 1,
            PageSize = 10
        };

        _mockAuditLogService
            .Setup(s => s.GetAuditLogsAsync(It.IsAny<AuditLogQueryRequest>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetAuditLogs(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetAuditLogById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var auditLogId = Guid.NewGuid();
        var auditLog = _autoFixture.Create<AuditLog>();
        auditLog.Id = auditLogId;

        _mockAuditLogService
            .Setup(s => s.GetAuditLogByIdAsync(auditLogId))
            .ReturnsAsync(auditLog);

        // Act
        var result = await _controller.GetAuditLogById(auditLogId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(auditLog);
        
        _mockAuditLogService.Verify(s => s.GetAuditLogByIdAsync(auditLogId), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var auditLogId = Guid.NewGuid();

        _mockAuditLogService
            .Setup(s => s.GetAuditLogByIdAsync(auditLogId))
            .ReturnsAsync((AuditLog?)null);

        // Act
        var result = await _controller.GetAuditLogById(auditLogId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        
        _mockAuditLogService.Verify(s => s.GetAuditLogByIdAsync(auditLogId), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsByUserId_WithValidUserId_ReturnsOkResult()
    {
        // Arrange
        var userId = "user123";
        var auditLogs = _autoFixture.CreateMany<AuditLog>(3).ToList();
        auditLogs.ForEach(log => log.UserId = userId);

        _mockAuditLogService
            .Setup(s => s.GetAuditLogsByUserIdAsync(userId))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _controller.GetAuditLogsByUserId(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(auditLogs);
        
        _mockAuditLogService.Verify(s => s.GetAuditLogsByUserIdAsync(userId), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetAuditLogsByUserId_WithInvalidUserId_ReturnsBadRequest(string userId)
    {
        // Act
        var result = await _controller.GetAuditLogsByUserId(userId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        _mockAuditLogService.Verify(s => s.GetAuditLogsByUserIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAuditLogsByActionType_WithValidActionType_ReturnsOkResult()
    {
        // Arrange
        var actionType = "Query";
        var auditLogs = _autoFixture.CreateMany<AuditLog>(3).ToList();
        auditLogs.ForEach(log => log.ActionType = actionType);

        _mockAuditLogService
            .Setup(s => s.GetAuditLogsByActionTypeAsync(actionType))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _controller.GetAuditLogsByActionType(actionType);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(auditLogs);
        
        _mockAuditLogService.Verify(s => s.GetAuditLogsByActionTypeAsync(actionType), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetAuditLogsByActionType_WithInvalidActionType_ReturnsBadRequest(string actionType)
    {
        // Act
        var result = await _controller.GetAuditLogsByActionType(actionType);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        _mockAuditLogService.Verify(s => s.GetAuditLogsByActionTypeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateAuditLog_WithValidRequest_ReturnsCreatedResult()
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

        var createdAuditLog = _autoFixture.Create<AuditLog>();
        createdAuditLog.ActionType = request.ActionType;
        createdAuditLog.ResourceId = request.ResourceId;

        _mockAuditLogService
            .Setup(s => s.CreateAuditLogAsync(It.IsAny<CreateAuditLogRequest>()))
            .ReturnsAsync(createdAuditLog);

        // Act
        var result = await _controller.CreateAuditLog(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdAuditLog);
        
        _mockAuditLogService.Verify(s => s.CreateAuditLogAsync(It.IsAny<CreateAuditLogRequest>()), Times.Once);
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
        var result = await _controller.CreateAuditLog(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        _mockAuditLogService.Verify(s => s.CreateAuditLogAsync(It.IsAny<CreateAuditLogRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAuditLog_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateAuditLogRequest
        {
            ActionType = "Query",
            ResourceId = "EVENT-123",
            ResourceType = "Event",
            Description = "User queried event details"
        };

        _mockAuditLogService
            .Setup(s => s.CreateAuditLogAsync(It.IsAny<CreateAuditLogRequest>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateAuditLog(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetAuditLogsSummary_ReturnsOkResult()
    {
        // Arrange
        var expectedSummary = new AuditLogsSummary
        {
            TotalLogs = 1000,
            LogsLast24Hours = 100,
            LogsLast7Days = 500,
            LogsLast30Days = 900,
            TopUsers = new[]
            {
                new UserActivitySummary { UserId = "user1", ActivityCount = 150 },
                new UserActivitySummary { UserId = "user2", ActivityCount = 120 },
                new UserActivitySummary { UserId = "user3", ActivityCount = 100 }
            },
            TopActionTypes = new[]
            {
                new ActionTypeSummary { ActionType = "Query", Count = 400 },
                new ActionTypeSummary { ActionType = "Replay", Count = 300 },
                new ActionTypeSummary { ActionType = "Export", Count = 200 }
            }
        };

        _mockAuditLogService
            .Setup(s => s.GetAuditLogsSummaryAsync())
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await _controller.GetAuditLogsSummary();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedSummary);
        
        _mockAuditLogService.Verify(s => s.GetAuditLogsSummaryAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAuditLogsSummary_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        _mockAuditLogService
            .Setup(s => s.GetAuditLogsSummaryAsync())
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetAuditLogsSummary();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}
