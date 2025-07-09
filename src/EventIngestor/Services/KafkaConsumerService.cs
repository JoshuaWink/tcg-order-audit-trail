using Confluent.Kafka;
using Microsoft.Extensions.Options;
using OrderAuditTrail.Shared.Configuration;
using OrderAuditTrail.EventIngestor.Services;
using System.Text.Json;
using OrderAuditTrail.Shared.Events;
using OrderAuditTrail.Shared.Services;

namespace OrderAuditTrail.EventIngestor.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly KafkaSettings _kafkaSettings;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMetricsCollector _metricsCollector;
    private IConsumer<string, string>? _consumer;

    public KafkaConsumerService(
        ILogger<KafkaConsumerService> logger,
        IOptions<KafkaSettings> kafkaSettings,
        IServiceProvider serviceProvider,
        IMetricsCollector metricsCollector)
    {
        _logger = logger;
        _kafkaSettings = kafkaSettings.Value;
        _serviceProvider = serviceProvider;
        _metricsCollector = metricsCollector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer Service starting");

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000,
            MaxPollIntervalMs = 300000,
            FetchMinBytes = 1,
            FetchWaitMaxMs = 500,
            SecurityProtocol = ParseSecurityProtocol(_kafkaSettings.SecurityProtocol),
            SaslMechanism = ParseSaslMechanism(_kafkaSettings.SaslMechanism),
            SaslUsername = _kafkaSettings.SaslUsername,
            SaslPassword = _kafkaSettings.SaslPassword
        };

        try
        {
            _consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, error) => _logger.LogError("Kafka error: {Error}", error.Reason))
                .SetStatisticsHandler((_, statistics) => _logger.LogDebug("Kafka statistics: {Statistics}", statistics))
                .Build();

            var topics = _kafkaSettings.Topics.Values.Select(t => t.Name).ToArray();
            _consumer.Subscribe(topics);

            _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", topics));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    
                    if (consumeResult?.Message != null)
                    {
                        await ProcessMessage(consumeResult, stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Error}", ex.Error.Reason);
                    _metricsCollector.IncrementConsumeErrors();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka consumer cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");
                    _metricsCollector.IncrementConsumeErrors();
                    
                    // Brief delay before continuing to avoid tight error loops
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in Kafka consumer service");
            throw;
        }
        finally
        {
            _consumer?.Close();
            _consumer?.Dispose();
            _logger.LogInformation("Kafka Consumer Service stopped");
        }
    }

    private async Task ProcessMessage(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Processing message from topic {Topic}, partition {Partition}, offset {Offset}", 
                consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);

            using var scope = _serviceProvider.CreateScope();
            var eventProcessor = scope.ServiceProvider.GetRequiredService<IEventProcessor>();

            var success = await eventProcessor.ProcessEventAsync(
                consumeResult.Message.Key,
                consumeResult.Message.Value,
                consumeResult.Topic,
                consumeResult.Partition,
                consumeResult.Offset,
                cancellationToken);

            if (success)
            {
                _consumer!.Commit(consumeResult);
                _metricsCollector.IncrementProcessedEvents("event", null);
                _logger.LogDebug("Successfully processed and committed message");
            }
            else
            {
                _logger.LogWarning("Failed to process message from topic {Topic}, partition {Partition}, offset {Offset}",
                    consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
                _metricsCollector.IncrementProcessingErrors("processing_error", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from topic {Topic}, partition {Partition}, offset {Offset}",
                consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
            _metricsCollector.IncrementProcessingErrors("exception_error", null);
        }
        finally
        {
            stopwatch.Stop();
            _metricsCollector.RecordProcessingTime("message_processing", stopwatch.Elapsed, null);
        }
    }

    private static SecurityProtocol ParseSecurityProtocol(string? protocol)
    {
        return protocol?.ToUpperInvariant() switch
        {
            "PLAINTEXT" => SecurityProtocol.Plaintext,
            "SSL" => SecurityProtocol.Ssl,
            "SASL_PLAINTEXT" => SecurityProtocol.SaslPlaintext,
            "SASL_SSL" => SecurityProtocol.SaslSsl,
            _ => SecurityProtocol.Plaintext
        };
    }

    private static SaslMechanism ParseSaslMechanism(string? mechanism)
    {
        return mechanism?.ToUpperInvariant() switch
        {
            "PLAIN" => SaslMechanism.Plain,
            "SCRAM-SHA-256" => SaslMechanism.ScramSha256,
            "SCRAM-SHA-512" => SaslMechanism.ScramSha512,
            "GSSAPI" => SaslMechanism.Gssapi,
            _ => SaslMechanism.Plain
        };
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Kafka Consumer Service");
        await base.StopAsync(cancellationToken);
    }
}
