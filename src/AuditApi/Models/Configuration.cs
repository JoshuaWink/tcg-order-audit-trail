namespace OrderAuditTrail.AuditApi.Models;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}

public class ApiSettings
{
    public int DefaultPageSize { get; set; } = 50;
    public int MaxPageSize { get; set; } = 1000;
    public int QueryTimeoutSeconds { get; set; } = 30;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public int MaxEventReplayDays { get; set; } = 30;
    public bool EnableSwagger { get; set; } = true;
}
