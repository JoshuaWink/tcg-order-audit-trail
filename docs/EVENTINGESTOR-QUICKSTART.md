# EventIngestor Quick Start Guide

## Prerequisites
- .NET 8 SDK
- Docker and Docker Compose
- PostgreSQL (or Docker)
- Apache Kafka (or Docker)

## Local Development Setup

### 1. Start Infrastructure
```bash
# Start PostgreSQL, Kafka, and Redis
docker-compose up -d postgres kafka redis

# Wait for services to be ready
docker-compose ps
```

### 2. Database Setup
```bash
# Apply database migrations
dotnet run --project migrations

# Or manually with psql
psql -h localhost -U postgres -d order_audit_trail -f migrations/001_initial_schema.sql
```

### 3. Configuration
```bash
# Copy environment file
cp .env.example .env

# Edit configuration as needed
# Key settings:
# - Database connection string
# - Kafka bootstrap servers
# - Redis connection string
```

### 4. Run EventIngestor
```bash
# Build and run
dotnet run --project src/EventIngestor

# Or with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/EventIngestor
```

## Docker Deployment

### Build Image
```bash
docker build -f src/EventIngestor/Dockerfile -t order-audit-trail/event-ingestor:latest .
```

### Run Container
```bash
docker run -d \
  --name event-ingestor \
  --network bridge \
  -p 8080:8080 \
  -p 9090:9090 \
  -e Database__ConnectionString="Host=postgres;Port=5432;Database=order_audit_trail;Username=postgres;Password=postgres" \
  -e Kafka__BootstrapServers="kafka:9092" \
  -e Redis__ConnectionString="redis:6379" \
  order-audit-trail/event-ingestor:latest
```

## Testing Event Processing

### Send Test Events
```bash
# Create test order event
kafka-console-producer --bootstrap-server localhost:9092 --topic order-events
```

### Sample Event
```json
{
  "eventId": "123e4567-e89b-12d3-a456-426614174000",
  "eventType": "OrderCreated",
  "aggregateId": "order-123",
  "aggregateType": "Order",
  "version": 1,
  "timestamp": "2025-01-08T10:00:00Z",
  "source": "OrderService",
  "customerId": "customer-456",
  "totalAmount": 99.99,
  "currency": "USD",
  "status": "PENDING",
  "items": [
    {
      "productId": "product-789",
      "quantity": 2,
      "unitPrice": 49.99
    }
  ]
}
```

## Monitoring

### Health Check
```bash
curl http://localhost:8080/health
```

### Metrics
```bash
curl http://localhost:9090/metrics
```

### Logs
```bash
# View logs
docker logs event-ingestor

# Follow logs
docker logs -f event-ingestor
```

## Common Issues

### Kafka Connection Issues
- Check Kafka is running: `docker-compose ps kafka`
- Verify bootstrap servers configuration
- Check network connectivity

### Database Connection Issues
- Verify PostgreSQL is running
- Check connection string format
- Ensure database exists and migrations are applied

### Event Processing Errors
- Check logs for validation errors
- Verify event format matches expected schema
- Check dead letter queue for failed events

## Development Tools

### Kafka Tools
```bash
# List topics
kafka-topics --bootstrap-server localhost:9092 --list

# Describe topic
kafka-topics --bootstrap-server localhost:9092 --describe --topic order-events

# Consumer group info
kafka-consumer-groups --bootstrap-server localhost:9092 --describe --group order-audit-trail-consumer-group
```

### Database Tools
```bash
# Connect to database
psql -h localhost -U postgres -d order_audit_trail

# Check events table
SELECT * FROM events ORDER BY created_at DESC LIMIT 10;

# Check dead letter queue
SELECT * FROM dead_letter_queue WHERE status = 'PENDING';
```

## Configuration Reference

### Environment Variables
- `Database__ConnectionString`: PostgreSQL connection string
- `Kafka__BootstrapServers`: Kafka broker addresses
- `Kafka__ConsumerGroupId`: Consumer group identifier
- `Redis__ConnectionString`: Redis connection string
- `ASPNETCORE_ENVIRONMENT`: Environment (Development/Production)

### Configuration Files
- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development overrides
- `appsettings.Production.json`: Production overrides
- `.env`: Environment-specific variables
