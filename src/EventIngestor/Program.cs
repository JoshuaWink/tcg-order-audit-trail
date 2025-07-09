using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderAuditTrail.Shared.Configuration;
using OrderAuditTrail.Shared.Data;
using OrderAuditTrail.Shared.Data.Repositories;
using OrderAuditTrail.EventIngestor.Services;
using OrderAuditTrail.EventIngestor.Validators;
using OrderAuditTrail.EventIngestor.Configuration;
using Serilog;
using FluentValidation;
using StackExchange.Redis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

// Configuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));

// Entity Framework
builder.Services.AddDbContext<AuditDbContext>((serviceProvider, options) =>
{
    var dbSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    options.UseNpgsql(dbSettings.ConnectionString);
});

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
{
    var redisSettings = serviceProvider.GetRequiredService<IOptions<RedisSettings>>().Value;
    return ConnectionMultiplexer.Connect(redisSettings.ConnectionString);
});

// Repositories
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventReplayRepository, EventReplayRepository>();
builder.Services.AddScoped<IDeadLetterQueueRepository, DeadLetterQueueRepository>();
builder.Services.AddScoped<IMetricsRepository, MetricsRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Services
builder.Services.AddScoped<IEventProcessor, EventProcessor>();
builder.Services.AddScoped<IEventPersistenceService, EventPersistenceService>();
builder.Services.AddScoped<IDeadLetterQueueService, DeadLetterQueueService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IEventValidationService, EventValidationService>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<EventValidator>();

// Kafka Consumer Service
builder.Services.AddHostedService<KafkaConsumerService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AuditDbContext>("database")
    .AddKafka(options =>
    {
        var kafkaSettings = builder.Configuration.GetSection("Kafka").Get<KafkaSettings>();
        options.BootstrapServers = kafkaSettings?.BootstrapServers ?? "localhost:9092";
    })
    .AddRedis(options =>
    {
        var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>();
        options.ConnectionString = redisSettings?.ConnectionString ?? "localhost:6379";
    });

// Metrics
builder.Services.AddSingleton<IMetricsCollector, MetricsCollector>();

var host = builder.Build();

// Ensure database is created
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    await context.Database.EnsureCreatedAsync();
}

try
{
    Log.Information("Starting EventIngestor service");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EventIngestor service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
