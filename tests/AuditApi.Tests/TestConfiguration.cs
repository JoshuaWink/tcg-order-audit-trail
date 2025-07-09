using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Data;
using OrderAuditTrail.Shared.Configuration;
using OrderAuditTrail.AuditApi;
using Testcontainers.PostgreSql;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OrderAuditTrail.AuditApi.Tests;

public class AuditApiTestFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly IConfiguration _configuration;

    public AuditApiTestFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("audit_trail_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        _configuration = BuildConfiguration();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        
        // Apply database migrations
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    private IConfiguration BuildConfiguration()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Database Configuration - using container values
                ["DatabaseSettings:Host"] = _postgresContainer.Hostname,
                ["DatabaseSettings:Port"] = _postgresContainer.GetMappedPublicPort(5432).ToString(),
                ["DatabaseSettings:Database"] = "audit_trail_test",
                ["DatabaseSettings:Username"] = "test_user",
                ["DatabaseSettings:Password"] = "test_password",
                ["DatabaseSettings:SslMode"] = "prefer",
                ["DatabaseSettings:MinPoolSize"] = "5",
                ["DatabaseSettings:MaxPoolSize"] = "100",
                ["DatabaseSettings:CommandTimeoutSeconds"] = "30",
                ["DatabaseSettings:ConnectionTimeoutSeconds"] = "15",
                
                // API Configuration - matching .env.example
                ["ApiSettings:Port"] = "5000",
                ["ApiSettings:CorsOrigins"] = "http://localhost:3000,http://localhost:8080",
                ["ApiSettings:RateLimitRequestsPerHour"] = "1000",
                ["ApiSettings:RateLimitReplayRequestsPerHour"] = "10",
                
                // Authentication Configuration - matching .env.example
                ["AuthenticationSettings:JwtSecretKey"] = "test-256-bit-secret-key-for-testing-change-this-in-production",
                ["AuthenticationSettings:JwtIssuer"] = "audit-trail-api",
                ["AuthenticationSettings:JwtAudience"] = "audit-trail-users",
                ["AuthenticationSettings:JwtExpirationMinutes"] = "60",
                
                // Logging Configuration - matching .env.example
                ["LoggingSettings:LogLevel"] = "Information",
                ["LoggingSettings:ConsoleEnabled"] = "true",
                ["LoggingSettings:FileEnabled"] = "false", // Disable file logging for tests
                ["LoggingSettings:FilePath"] = "logs/audit-trail-test.log",
                ["LoggingSettings:FileMaxSizeMB"] = "100",
                ["LoggingSettings:FileMaxFiles"] = "10",
                
                // Monitoring Configuration - matching .env.example
                ["MonitoringSettings:MetricsEnabled"] = "true",
                ["MonitoringSettings:MetricsPort"] = "9090",
                ["MonitoringSettings:HealthCheckPath"] = "/health",
                ["MonitoringSettings:HealthCheckTimeoutSeconds"] = "30",
                
                // Encryption Configuration - matching .env.example
                ["EncryptionSettings:Key"] = "test-32-character-encryption-key-here-change-this",
                ["EncryptionSettings:IV"] = "test-16-char-iv-here",
                
                // Performance Configuration - matching .env.example
                ["PerformanceSettings:ConnectionPoolMinSize"] = "5",
                ["PerformanceSettings:ConnectionPoolMaxSize"] = "100",
                ["PerformanceSettings:CommandTimeoutSeconds"] = "30",
                ["PerformanceSettings:ConnectionTimeoutSeconds"] = "15",
                
                // Caching Configuration - matching .env.example
                ["CachingSettings:RedisHost"] = "localhost",
                ["CachingSettings:RedisPort"] = "6379",
                ["CachingSettings:RedisPassword"] = "",
                ["CachingSettings:RedisDatabase"] = "0",
                ["CachingSettings:RedisSslEnabled"] = "false",
                ["CachingSettings:CacheTtlSeconds"] = "300",
                ["CachingSettings:CacheEnabled"] = "false", // Disable caching for tests
                
                // Development/Testing Configuration - matching .env.example
                ["DevelopmentSettings:DevelopmentMode"] = "true",
                ["DevelopmentSettings:SeedTestData"] = "false",
                ["DevelopmentSettings:TestDatabaseName"] = "audit_trail_test",
                ["DevelopmentSettings:IntegrationTestMode"] = "true"
            });

        return configBuilder.Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuditDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add test database context
            services.AddDbContext<AuditDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // Configure JWT authentication for testing
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "audit-trail-api",
                        ValidAudience = "audit-trail-users",
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes("test-256-bit-secret-key-for-testing-change-this-in-production"))
                    };
                });
        });

        builder.UseConfiguration(_configuration);
        builder.UseEnvironment("Testing");
    }

    public string GenerateJwtToken(string userId = "test-user", IEnumerable<string>? roles = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("test-256-bit-secret-key-for-testing-change-this-in-production");
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (roles != null)
        {
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "audit-trail-api",
            Audience = "audit-trail-users",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public HttpClient CreateAuthenticatedClient(string userId = "test-user", IEnumerable<string>? roles = null)
    {
        var client = CreateClient();
        var token = GenerateJwtToken(userId, roles);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public string PostgresConnectionString => _postgresContainer.GetConnectionString();
    public new IConfiguration Configuration => _configuration;

    public new async ValueTask DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}

public class AuditApiTestFixture : IAsyncLifetime
{
    public AuditApiTestFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new AuditApiTestFactory();
        await Factory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }
    }
}
