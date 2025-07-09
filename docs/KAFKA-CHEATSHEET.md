# Kafka Cheatsheet

## Overview

This cheatsheet provides quick reference for working with Kafka in the Order Event Tracking & Audit Trail System.

## Topic Structure

Our Kafka topics follow a consistent naming convention:

```
{domain}.{entity}.{eventType}
```

### Standard Topics

| Topic | Description | Partitions | Retention |
|-------|-------------|------------|-----------|
| `orders.order.created` | Order creation events | 10 | 30 days |
| `orders.order.updated` | Order update events | 10 | 30 days |
| `orders.order.cancelled` | Order cancellation events | 10 | 30 days |
| `orders.order.completed` | Order completion events | 10 | 30 days |
| `inventory.item.updated` | Inventory changes | 5 | 30 days |
| `payments.payment.processed` | Payment processing events | 10 | 90 days |
| `payments.payment.failed` | Payment failures | 5 | 90 days |
| `shipping.shipment.created` | Shipment creation | 5 | 30 days |
| `shipping.shipment.delivered` | Delivery confirmations | 5 | 30 days |

### Dead Letter Topics

For failed message processing:

| Topic | Description |
|-------|-------------|
| `orders.order.created.dlq` | Failed order creation processing |
| `orders.order.updated.dlq` | Failed order update processing |
| `inventory.item.updated.dlq` | Failed inventory processing |
| `payments.payment.processed.dlq` | Failed payment processing |

## Common Commands

### Topic Management

```bash
# List all topics
kafka-topics --list --bootstrap-server localhost:9092

# Create a topic
kafka-topics --create \
  --topic orders.order.created \
  --partitions 10 \
  --replication-factor 3 \
  --bootstrap-server localhost:9092

# Describe a topic
kafka-topics --describe \
  --topic orders.order.created \
  --bootstrap-server localhost:9092

# Delete a topic
kafka-topics --delete \
  --topic orders.order.created \
  --bootstrap-server localhost:9092
```

### Consumer Groups

```bash
# List consumer groups
kafka-consumer-groups --list --bootstrap-server localhost:9092

# Describe consumer group
kafka-consumer-groups --describe \
  --group audit-trail-ingestor \
  --bootstrap-server localhost:9092

# Reset consumer group offset
kafka-consumer-groups --reset-offsets \
  --group audit-trail-ingestor \
  --topic orders.order.created \
  --to-earliest \
  --bootstrap-server localhost:9092 \
  --execute
```

### Monitoring

```bash
# Check topic lag
kafka-consumer-groups --describe \
  --group audit-trail-ingestor \
  --bootstrap-server localhost:9092

# View topic configuration
kafka-configs --describe \
  --entity-type topics \
  --entity-name orders.order.created \
  --bootstrap-server localhost:9092

# Get topic partition information
kafka-log-dirs --describe \
  --bootstrap-server localhost:9092 \
  --topic-list orders.order.created
```

## Event Schema

### Standard Event Structure

All events follow this JSON schema:

```json
{
  "eventId": "uuid",
  "eventType": "string",
  "aggregateId": "string", 
  "aggregateType": "string",
  "timestamp": "2025-01-08T10:30:00Z",
  "version": 1,
  "payload": {
    "orderId": "order-123",
    "customerId": "customer-456",
    "amount": 99.99,
    "currency": "USD"
  },
  "metadata": {
    "correlationId": "correlation-789",
    "causationId": "command-101",
    "userId": "user-123",
    "source": "OrderService"
  }
}
```

### Event Examples

#### Order Created Event

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "eventType": "OrderCreated",
  "aggregateId": "order-123",
  "aggregateType": "Order",
  "timestamp": "2025-01-08T10:30:00Z",
  "version": 1,
  "payload": {
    "orderId": "order-123",
    "customerId": "customer-456",
    "items": [
      {
        "productId": "product-789",
        "quantity": 2,
        "price": 49.99
      }
    ],
    "totalAmount": 99.98,
    "currency": "USD",
    "shippingAddress": {
      "street": "123 Main St",
      "city": "Anytown",
      "state": "NY",
      "zipCode": "12345"
    }
  },
  "metadata": {
    "correlationId": "correlation-789",
    "causationId": "create-order-command-101",
    "userId": "user-123",
    "source": "OrderService"
  }
}
```

#### Payment Processed Event

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440001",
  "eventType": "PaymentProcessed",
  "aggregateId": "payment-456",
  "aggregateType": "Payment",
  "timestamp": "2025-01-08T10:35:00Z",
  "version": 1,
  "payload": {
    "paymentId": "payment-456",
    "orderId": "order-123",
    "amount": 99.98,
    "currency": "USD",
    "paymentMethod": "credit_card",
    "transactionId": "txn-789",
    "status": "completed"
  },
  "metadata": {
    "correlationId": "correlation-789",
    "causationId": "process-payment-command-102",
    "userId": "user-123",
    "source": "PaymentService"
  }
}
```

## Producer Configuration

### .NET Producer Settings

```csharp
var config = new ProducerConfig
{
    BootstrapServers = "localhost:9092",
    ClientId = "audit-trail-producer",
    
    // Reliability settings
    Acks = Acks.All,
    EnableIdempotence = true,
    MessageTimeoutMs = 10000,
    
    // Performance settings
    CompressionType = CompressionType.Snappy,
    BatchSize = 16384,
    LingerMs = 5,
    
    // Serialization
    KeySerializer = Serializers.Utf8,
    ValueSerializer = Serializers.Utf8
};
```

### Producer Best Practices

```csharp
// Use correlation IDs for tracing
var headers = new Headers
{
    { "correlationId", Encoding.UTF8.GetBytes(correlationId) },
    { "source", Encoding.UTF8.GetBytes("OrderService") }
};

// Partition by aggregate ID for ordering
var message = new Message<string, string>
{
    Key = aggregateId,
    Value = JsonSerializer.Serialize(eventData),
    Headers = headers,
    Timestamp = new Timestamp(DateTime.UtcNow)
};

await producer.ProduceAsync("orders.order.created", message);
```

## Consumer Configuration

### .NET Consumer Settings

```csharp
var config = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "audit-trail-ingestor",
    ClientId = "audit-trail-consumer",
    
    // Offset management
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false,
    
    // Performance settings
    FetchMinBytes = 1,
    FetchMaxWaitMs = 500,
    MaxPollIntervalMs = 300000,
    
    // Deserialization
    KeyDeserializer = Deserializers.Utf8,
    ValueDeserializer = Deserializers.Utf8
};
```

### Consumer Best Practices

```csharp
// Manual commit for reliability
using var consumer = new ConsumerBuilder<string, string>(config).Build();
consumer.Subscribe(topics);

while (!cancellationToken.IsCancellationRequested)
{
    var consumeResult = consumer.Consume(cancellationToken);
    
    try
    {
        await ProcessEvent(consumeResult.Message.Value);
        consumer.Commit(consumeResult);
    }
    catch (Exception ex)
    {
        // Handle error, possibly send to DLQ
        await SendToDeadLetterQueue(consumeResult.Message, ex);
    }
}
```

## Error Handling

### Dead Letter Queue Pattern

```csharp
private async Task SendToDeadLetterQueue(Message<string, string> originalMessage, Exception error)
{
    var dlqTopic = $"{originalMessage.Topic}.dlq";
    var dlqMessage = new
    {
        OriginalTopic = originalMessage.Topic,
        OriginalMessage = originalMessage.Value,
        ErrorMessage = error.Message,
        ErrorTimestamp = DateTime.UtcNow,
        RetryCount = GetRetryCount(originalMessage) + 1
    };
    
    await dlqProducer.ProduceAsync(dlqTopic, new Message<string, string>
    {
        Key = originalMessage.Key,
        Value = JsonSerializer.Serialize(dlqMessage),
        Headers = originalMessage.Headers
    });
}
```

### Retry Logic

```csharp
private async Task<bool> ProcessWithRetry(Message<string, string> message, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await ProcessEvent(message.Value);
            return true;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            await Task.Delay(delay);
        }
    }
    return false;
}
```

## Monitoring & Debugging

### Key Metrics to Monitor

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| Consumer Lag | Messages behind latest offset | > 1000 |
| Processing Rate | Messages per second | < 10/sec |
| Error Rate | Failed messages percentage | > 5% |
| DLQ Size | Messages in dead letter queue | > 100 |

### Troubleshooting Commands

```bash
# Check consumer lag
kafka-consumer-groups --bootstrap-server localhost:9092 \
  --describe --group audit-trail-ingestor

# Tail a topic
kafka-console-consumer --bootstrap-server localhost:9092 \
  --topic orders.order.created \
  --from-beginning

# Check partition distribution
kafka-topics --bootstrap-server localhost:9092 \
  --describe --topic orders.order.created

# View topic messages with keys
kafka-console-consumer --bootstrap-server localhost:9092 \
  --topic orders.order.created \
  --property print.key=true \
  --property key.separator=:
```

## Local Development

### Docker Compose Setup

```yaml
version: '3.8'
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.4.0
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000

  kafka:
    image: confluentinc/cp-kafka:7.4.0
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1

  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    depends_on:
      - kafka
    ports:
      - "8080:8080"
    environment:
      KAFKA_CLUSTERS_0_NAME: local
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: kafka:9092
```

### Topic Creation Script

```bash
#!/bin/bash
# create-topics.sh

KAFKA_SERVER="localhost:9092"

# Order topics
kafka-topics --create --if-not-exists --topic orders.order.created --partitions 10 --replication-factor 1 --bootstrap-server $KAFKA_SERVER
kafka-topics --create --if-not-exists --topic orders.order.updated --partitions 10 --replication-factor 1 --bootstrap-server $KAFKA_SERVER
kafka-topics --create --if-not-exists --topic orders.order.cancelled --partitions 10 --replication-factor 1 --bootstrap-server $KAFKA_SERVER

# Payment topics
kafka-topics --create --if-not-exists --topic payments.payment.processed --partitions 10 --replication-factor 1 --bootstrap-server $KAFKA_SERVER
kafka-topics --create --if-not-exists --topic payments.payment.failed --partitions 5 --replication-factor 1 --bootstrap-server $KAFKA_SERVER

# DLQ topics
kafka-topics --create --if-not-exists --topic orders.order.created.dlq --partitions 1 --replication-factor 1 --bootstrap-server $KAFKA_SERVER
kafka-topics --create --if-not-exists --topic orders.order.updated.dlq --partitions 1 --replication-factor 1 --bootstrap-server $KAFKA_SERVER

echo "Topics created successfully"
```

## Performance Tuning

### Producer Optimization

```csharp
var config = new ProducerConfig
{
    // Batch multiple records together
    BatchSize = 65536,
    LingerMs = 10,
    
    // Compress messages
    CompressionType = CompressionType.Lz4,
    
    // Memory optimization
    BufferMemory = 33554432,
    
    // Network optimization
    SendBufferBytes = 131072,
    ReceiveBufferBytes = 65536
};
```

### Consumer Optimization

```csharp
var config = new ConsumerConfig
{
    // Fetch more data per request
    FetchMinBytes = 50000,
    FetchMaxWaitMs = 500,
    
    // Process in larger batches
    MaxPollRecords = 500,
    
    // Memory optimization
    ReceiveBufferBytes = 65536,
    SendBufferBytes = 131072
};
```

## Security

### SASL/SCRAM Configuration

```csharp
var config = new ProducerConfig
{
    BootstrapServers = "localhost:9092",
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslMechanism = SaslMechanism.ScramSha256,
    SaslUsername = "your-username",
    SaslPassword = "your-password"
};
```

### SSL Configuration

```csharp
var config = new ProducerConfig
{
    BootstrapServers = "localhost:9092",
    SecurityProtocol = SecurityProtocol.Ssl,
    SslCaLocation = "/path/to/ca-cert",
    SslCertificateLocation = "/path/to/client-cert",
    SslKeyLocation = "/path/to/client-key"
};
```
