using System.Diagnostics;
using System.Text;

namespace OrderAuditTrail.AuditApi.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            // Log request
            await LogRequestAsync(context);

            // Create a new memory stream for the response body
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware
            await _next(context);

            // Log response
            await LogResponseAsync(context, stopwatch.ElapsedMilliseconds);

            // Copy the response body to the original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing request {Method} {Path}",
                context.Request.Method, context.Request.Path);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
            stopwatch.Stop();
        }
    }

    private async Task LogRequestAsync(HttpContext context)
    {
        var request = context.Request;
        var correlationId = context.Items["CorrelationId"]?.ToString();

        _logger.LogInformation(
            "HTTP {Method} {Path} started - CorrelationId: {CorrelationId}, UserAgent: {UserAgent}, RemoteIP: {RemoteIP}",
            request.Method,
            request.Path,
            correlationId,
            request.Headers["User-Agent"].FirstOrDefault(),
            GetClientIpAddress(context));

        // Log request body for POST/PUT requests (be careful with sensitive data)
        if (request.Method == "POST" || request.Method == "PUT")
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var requestBody = Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0;

            // Only log if body is not too large and doesn't contain sensitive data
            if (requestBody.Length <= 1000 && !ContainsSensitiveData(requestBody))
            {
                _logger.LogDebug("Request body: {RequestBody}", requestBody);
            }
        }
    }

    private async Task LogResponseAsync(HttpContext context, long elapsedMilliseconds)
    {
        var response = context.Response;
        var correlationId = context.Items["CorrelationId"]?.ToString();

        response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        _logger.LogInformation(
            "HTTP {Method} {Path} completed - Status: {StatusCode}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            response.StatusCode,
            elapsedMilliseconds,
            correlationId);

        // Log response body for errors or debug level
        if (response.StatusCode >= 400 || _logger.IsEnabled(LogLevel.Debug))
        {
            if (responseBody.Length <= 1000)
            {
                _logger.LogDebug("Response body: {ResponseBody}", responseBody);
            }
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool ContainsSensitiveData(string content)
    {
        var sensitiveFields = new[] { "password", "token", "secret", "key", "authorization" };
        return sensitiveFields.Any(field => content.Contains(field, StringComparison.OrdinalIgnoreCase));
    }
}

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        context.Items["CorrelationId"] = correlationId;

        // Add correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            return Task.CompletedTask;
        });

        // Add correlation ID to structured logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        return correlationId;
    }
}
