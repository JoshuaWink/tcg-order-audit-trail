using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OrderAuditTrail.AuditApi.Middleware;
using FluentAssertions;
using System.Text;

namespace OrderAuditTrail.AuditApi.Tests.Middleware;

public class RequestLoggingMiddlewareTests
{
    private readonly Mock<ILogger<RequestLoggingMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly RequestLoggingMiddleware _middleware;

    public RequestLoggingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new RequestLoggingMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithValidRequest_LogsRequestAndResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/events";
        context.Request.QueryString = new QueryString("?page=1&pageSize=10");
        context.Request.Headers["User-Agent"] = "TestAgent";
        context.Request.Headers["Authorization"] = "Bearer token123";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        var responseBody = "test response";
        var responseBodyBytes = Encoding.UTF8.GetBytes(responseBody);

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback<HttpContext>(ctx =>
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                ctx.Response.Body.WriteAsync(responseBodyBytes, 0, responseBodyBytes.Length);
            })
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        
        // Verify that logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Response")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithException_LogsExceptionAndRethrows()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/events";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        var expectedException = new Exception("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<Exception>(() => _middleware.InvokeAsync(context));
        actualException.Should().Be(expectedException);

        _mockNext.Verify(x => x(context), Times.Once);
        
        // Verify that request logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify that error logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSensitiveHeaders_RedactsAuthorizationHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/events";
        context.Request.Headers["Authorization"] = "Bearer supersecrettoken123";
        context.Request.Headers["User-Agent"] = "TestAgent";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback<HttpContext>(ctx => ctx.Response.StatusCode = 200)
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        
        // Verify that sensitive data was redacted in logs
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Authorization: [REDACTED]")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithDifferentHttpMethods_LogsCorrectly()
    {
        // Arrange
        var methods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
        
        foreach (var method in methods)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = "/api/test";
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx => ctx.Response.StatusCode = 200)
                .Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Method: {method}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    [Fact]
    public async Task InvokeAsync_WithDifferentStatusCodes_LogsCorrectly()
    {
        // Arrange
        var statusCodes = new[] { 200, 201, 400, 401, 404, 500 };
        
        foreach (var statusCode in statusCodes)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/api/test";
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

            _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(ctx => ctx.Response.StatusCode = statusCode)
                .Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Status: {statusCode}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    [Fact]
    public async Task InvokeAsync_WithoutRemoteIpAddress_LogsWithoutError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/events";
        // Note: Not setting RemoteIpAddress to simulate case where it's null

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback<HttpContext>(ctx => ctx.Response.StatusCode = 200)
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        
        // Verify that logging was called even without IP address
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithLongRunningRequest_LogsExecutionTime()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/events";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback<HttpContext>(async ctx =>
            {
                await Task.Delay(100); // Simulate processing time
                ctx.Response.StatusCode = 200;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        
        // Verify that execution time was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Duration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
