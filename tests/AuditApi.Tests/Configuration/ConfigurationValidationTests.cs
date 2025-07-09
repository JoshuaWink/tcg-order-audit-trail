using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderAuditTrail.Shared.Configuration;
using FluentAssertions;

namespace OrderAuditTrail.AuditApi.Tests.Configuration;

public class ConfigurationValidationTests : IClassFixture<AuditApiTestFixture>
{
    private readonly AuditApiTestFixture _fixture;

    public ConfigurationValidationTests(AuditApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Configuration_ShouldHaveAllRequiredDatabaseSettings()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        configuration["DatabaseSettings:Host"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:Port"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:Database"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:Username"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:Password"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:SslMode"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:MinPoolSize"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:MaxPoolSize"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:CommandTimeoutSeconds"].Should().NotBeNullOrEmpty();
        configuration["DatabaseSettings:ConnectionTimeoutSeconds"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldHaveAllRequiredApiSettings()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        configuration["ApiSettings:Port"].Should().NotBeNullOrEmpty();
        configuration["ApiSettings:CorsOrigins"].Should().NotBeNullOrEmpty();
        configuration["ApiSettings:RateLimitRequestsPerHour"].Should().NotBeNullOrEmpty();
        configuration["ApiSettings:RateLimitReplayRequestsPerHour"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldHaveAllRequiredAuthenticationSettings()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        configuration["AuthenticationSettings:JwtSecretKey"].Should().NotBeNullOrEmpty();
        configuration["AuthenticationSettings:JwtIssuer"].Should().NotBeNullOrEmpty();
        configuration["AuthenticationSettings:JwtAudience"].Should().NotBeNullOrEmpty();
        configuration["AuthenticationSettings:JwtExpirationMinutes"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldHaveAllRequiredLoggingSettings()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        configuration["LoggingSettings:LogLevel"].Should().NotBeNullOrEmpty();
        configuration["LoggingSettings:ConsoleEnabled"].Should().NotBeNullOrEmpty();
        configuration["LoggingSettings:FileEnabled"].Should().NotBeNullOrEmpty();
        configuration["LoggingSettings:FilePath"].Should().NotBeNullOrEmpty();
        configuration["LoggingSettings:FileMaxSizeMB"].Should().NotBeNullOrEmpty();
        configuration["LoggingSettings:FileMaxFiles"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldHaveAllRequiredMonitoringSettings()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        configuration["MonitoringSettings:MetricsEnabled"].Should().NotBeNullOrEmpty();
        configuration["MonitoringSettings:MetricsPort"].Should().NotBeNullOrEmpty();
        configuration["MonitoringSettings:HealthCheckPath"].Should().NotBeNullOrEmpty();
        configuration["MonitoringSettings:HealthCheckTimeoutSeconds"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldHaveAllRequiredEncryptionSettings()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        configuration["EncryptionSettings:Key"].Should().NotBeNullOrEmpty();
        configuration["EncryptionSettings:IV"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldHaveAllRequiredPerformanceSettings()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        configuration["PerformanceSettings:ConnectionPoolMinSize"].Should().NotBeNullOrEmpty();
        configuration["PerformanceSettings:ConnectionPoolMaxSize"].Should().NotBeNullOrEmpty();
        configuration["PerformanceSettings:CommandTimeoutSeconds"].Should().NotBeNullOrEmpty();
        configuration["PerformanceSettings:ConnectionTimeoutSeconds"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldHaveAllRequiredCachingSettings()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        configuration["CachingSettings:RedisHost"].Should().NotBeNullOrEmpty();
        configuration["CachingSettings:RedisPort"].Should().NotBeNullOrEmpty();
        configuration["CachingSettings:RedisDatabase"].Should().NotBeNullOrEmpty();
        configuration["CachingSettings:RedisSslEnabled"].Should().NotBeNullOrEmpty();
        configuration["CachingSettings:CacheTtlSeconds"].Should().NotBeNullOrEmpty();
        configuration["CachingSettings:CacheEnabled"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldHaveAllRequiredDevelopmentSettings()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        configuration["DevelopmentSettings:DevelopmentMode"].Should().NotBeNullOrEmpty();
        configuration["DevelopmentSettings:SeedTestData"].Should().NotBeNullOrEmpty();
        configuration["DevelopmentSettings:TestDatabaseName"].Should().NotBeNullOrEmpty();
        configuration["DevelopmentSettings:IntegrationTestMode"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_DatabaseSettings_ShouldHaveValidNumericValues()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        int.TryParse(configuration["DatabaseSettings:Port"], out var port).Should().BeTrue();
        port.Should().BeGreaterThan(0).And.BeLessThan(65536);

        int.TryParse(configuration["DatabaseSettings:MinPoolSize"], out var minPoolSize).Should().BeTrue();
        minPoolSize.Should().BeGreaterThan(0);

        int.TryParse(configuration["DatabaseSettings:MaxPoolSize"], out var maxPoolSize).Should().BeTrue();
        maxPoolSize.Should().BeGreaterThan(minPoolSize);

        int.TryParse(configuration["DatabaseSettings:CommandTimeoutSeconds"], out var commandTimeout).Should().BeTrue();
        commandTimeout.Should().BeGreaterThan(0);

        int.TryParse(configuration["DatabaseSettings:ConnectionTimeoutSeconds"], out var connectionTimeout).Should().BeTrue();
        connectionTimeout.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Configuration_ApiSettings_ShouldHaveValidNumericValues()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        int.TryParse(configuration["ApiSettings:Port"], out var port).Should().BeTrue();
        port.Should().BeGreaterThan(0).And.BeLessThan(65536);

        int.TryParse(configuration["ApiSettings:RateLimitRequestsPerHour"], out var rateLimitRequests).Should().BeTrue();
        rateLimitRequests.Should().BeGreaterThan(0);

        int.TryParse(configuration["ApiSettings:RateLimitReplayRequestsPerHour"], out var rateLimitReplay).Should().BeTrue();
        rateLimitReplay.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Configuration_AuthenticationSettings_ShouldHaveValidValues()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        var jwtSecretKey = configuration["AuthenticationSettings:JwtSecretKey"];
        jwtSecretKey.Should().NotBeNullOrEmpty();
        jwtSecretKey!.Length.Should().BeGreaterOrEqualTo(32); // Minimum length for security

        var jwtIssuer = configuration["AuthenticationSettings:JwtIssuer"];
        jwtIssuer.Should().NotBeNullOrEmpty();

        var jwtAudience = configuration["AuthenticationSettings:JwtAudience"];
        jwtAudience.Should().NotBeNullOrEmpty();

        int.TryParse(configuration["AuthenticationSettings:JwtExpirationMinutes"], out var expiration).Should().BeTrue();
        expiration.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Configuration_LoggingSettings_ShouldHaveValidValues()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        var logLevel = configuration["LoggingSettings:LogLevel"];
        logLevel.Should().NotBeNullOrEmpty();
        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        validLogLevels.Should().Contain(logLevel);

        bool.TryParse(configuration["LoggingSettings:ConsoleEnabled"], out var consoleEnabled).Should().BeTrue();
        bool.TryParse(configuration["LoggingSettings:FileEnabled"], out var fileEnabled).Should().BeTrue();

        int.TryParse(configuration["LoggingSettings:FileMaxSizeMB"], out var maxSize).Should().BeTrue();
        maxSize.Should().BeGreaterThan(0);

        int.TryParse(configuration["LoggingSettings:FileMaxFiles"], out var maxFiles).Should().BeTrue();
        maxFiles.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Configuration_MonitoringSettings_ShouldHaveValidValues()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        bool.TryParse(configuration["MonitoringSettings:MetricsEnabled"], out var metricsEnabled).Should().BeTrue();

        int.TryParse(configuration["MonitoringSettings:MetricsPort"], out var metricsPort).Should().BeTrue();
        metricsPort.Should().BeGreaterThan(0).And.BeLessThan(65536);

        var healthCheckPath = configuration["MonitoringSettings:HealthCheckPath"];
        healthCheckPath.Should().NotBeNullOrEmpty();
        healthCheckPath.Should().StartWith("/");

        int.TryParse(configuration["MonitoringSettings:HealthCheckTimeoutSeconds"], out var healthCheckTimeout).Should().BeTrue();
        healthCheckTimeout.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Configuration_EncryptionSettings_ShouldHaveValidValues()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        var encryptionKey = configuration["EncryptionSettings:Key"];
        encryptionKey.Should().NotBeNullOrEmpty();
        encryptionKey!.Length.Should().BeGreaterOrEqualTo(32); // Minimum length for AES-256

        var encryptionIV = configuration["EncryptionSettings:IV"];
        encryptionIV.Should().NotBeNullOrEmpty();
        encryptionIV!.Length.Should().BeGreaterOrEqualTo(16); // Minimum length for AES IV
    }

    [Fact]
    public void Configuration_CachingSettings_ShouldHaveValidValues()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        var redisHost = configuration["CachingSettings:RedisHost"];
        redisHost.Should().NotBeNullOrEmpty();

        int.TryParse(configuration["CachingSettings:RedisPort"], out var redisPort).Should().BeTrue();
        redisPort.Should().BeGreaterThan(0).And.BeLessThan(65536);

        int.TryParse(configuration["CachingSettings:RedisDatabase"], out var redisDatabase).Should().BeTrue();
        redisDatabase.Should().BeGreaterOrEqualTo(0);

        bool.TryParse(configuration["CachingSettings:RedisSslEnabled"], out var sslEnabled).Should().BeTrue();
        bool.TryParse(configuration["CachingSettings:CacheEnabled"], out var cacheEnabled).Should().BeTrue();

        int.TryParse(configuration["CachingSettings:CacheTtlSeconds"], out var cacheTtl).Should().BeTrue();
        cacheTtl.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Configuration_DevelopmentSettings_ShouldHaveValidValues()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        bool.TryParse(configuration["DevelopmentSettings:DevelopmentMode"], out var developmentMode).Should().BeTrue();
        bool.TryParse(configuration["DevelopmentSettings:SeedTestData"], out var seedTestData).Should().BeTrue();
        bool.TryParse(configuration["DevelopmentSettings:IntegrationTestMode"], out var integrationTestMode).Should().BeTrue();

        var testDatabaseName = configuration["DevelopmentSettings:TestDatabaseName"];
        testDatabaseName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_ShouldBeConsistentWithEnvExample()
    {
        // This test ensures that our test configuration covers all the keys from .env.example
        // This would need to be updated based on the actual .env.example file structure
        
        // Arrange
        var configuration = _fixture.Factory.Configuration;
        var allKeys = new[]
        {
            "DatabaseSettings:Host",
            "DatabaseSettings:Port",
            "DatabaseSettings:Database",
            "DatabaseSettings:Username",
            "DatabaseSettings:Password",
            "DatabaseSettings:SslMode",
            "DatabaseSettings:MinPoolSize",
            "DatabaseSettings:MaxPoolSize",
            "DatabaseSettings:CommandTimeoutSeconds",
            "DatabaseSettings:ConnectionTimeoutSeconds",
            "ApiSettings:Port",
            "ApiSettings:CorsOrigins",
            "ApiSettings:RateLimitRequestsPerHour",
            "ApiSettings:RateLimitReplayRequestsPerHour",
            "AuthenticationSettings:JwtSecretKey",
            "AuthenticationSettings:JwtIssuer",
            "AuthenticationSettings:JwtAudience",
            "AuthenticationSettings:JwtExpirationMinutes",
            "LoggingSettings:LogLevel",
            "LoggingSettings:ConsoleEnabled",
            "LoggingSettings:FileEnabled",
            "LoggingSettings:FilePath",
            "LoggingSettings:FileMaxSizeMB",
            "LoggingSettings:FileMaxFiles",
            "MonitoringSettings:MetricsEnabled",
            "MonitoringSettings:MetricsPort",
            "MonitoringSettings:HealthCheckPath",
            "MonitoringSettings:HealthCheckTimeoutSeconds",
            "EncryptionSettings:Key",
            "EncryptionSettings:IV",
            "PerformanceSettings:ConnectionPoolMinSize",
            "PerformanceSettings:ConnectionPoolMaxSize",
            "PerformanceSettings:CommandTimeoutSeconds",
            "PerformanceSettings:ConnectionTimeoutSeconds",
            "CachingSettings:RedisHost",
            "CachingSettings:RedisPort",
            "CachingSettings:RedisPassword",
            "CachingSettings:RedisDatabase",
            "CachingSettings:RedisSslEnabled",
            "CachingSettings:CacheTtlSeconds",
            "CachingSettings:CacheEnabled",
            "DevelopmentSettings:DevelopmentMode",
            "DevelopmentSettings:SeedTestData",
            "DevelopmentSettings:TestDatabaseName",
            "DevelopmentSettings:IntegrationTestMode"
        };

        // Act & Assert
        foreach (var key in allKeys)
        {
            var value = configuration[key];
            value.Should().NotBeNull($"Configuration key '{key}' should be present");
        }
    }

    [Fact]
    public void Configuration_ShouldBeValidForTestEnvironment()
    {
        // Arrange
        var configuration = _fixture.Factory.Configuration;

        // Act & Assert
        // Verify test-specific settings
        configuration["DevelopmentSettings:IntegrationTestMode"].Should().Be("true");
        configuration["LoggingSettings:FileEnabled"].Should().Be("false"); // File logging should be disabled in tests
        configuration["CachingSettings:CacheEnabled"].Should().Be("false"); // Caching should be disabled in tests
        configuration["DevelopmentSettings:SeedTestData"].Should().Be("false"); // No seed data in tests
    }
}
