# Architecture & Design

## Overview

The Order Event Tracking & Audit Trail System is built as a distributed, event-driven architecture that captures, stores, and exposes an immutable history of all business events across our platform.

## System Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Microservice  │    │   Microservice  │    │   Microservice  │
│   (Orders)      │    │   (Inventory)   │    │   (Payments)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                         ┌───────▼───────┐
                         │     Kafka     │
                         │   (Topics)    │
                         └───────────────┘
                                 │
                         ┌───────▼───────┐
                         │ EventIngestor │
                         │   Service     │
                         └───────────────┘
                                 │
                         ┌───────▼───────┐
                         │  PostgreSQL   │
                         │  Event Store  │
                         └───────────────┘
                                 │
                         ┌───────▼───────┐
                         │   AuditApi    │
                         │   Service     │
                         └───────────────┘
                                 │
                    ┌────────────┼────────────┐
                    │            │            │
            ┌───────▼────┐ ┌─────▼─────┐ ┌───▼────┐
            │ Compliance │ │ Analytics │ │ Debug  │
            │   Tools    │ │   Tools   │ │ Tools  │
            └────────────┘ └───────────┘ └────────┘
```

## Core Components

### EventIngestor Service

**Purpose**: Consumes events from Kafka and persists them to the event store.

**Key Responsibilities**:
- Subscribe to configured Kafka topics
- Validate and deserialize incoming events
- Store events in PostgreSQL with immutable guarantees
- Handle dead letter queues for failed events
- Provide health checks and metrics

**Technology Stack**:
- .NET 8 with hosted service pattern
- Confluent.Kafka for Kafka integration
- Entity Framework Core for database access
- Serilog for structured logging

### AuditApi Service

**Purpose**: Provides REST API for querying and replaying events.

**Key Responsibilities**:
- Expose RESTful endpoints for event querying
- Support filtering by time range, event type, entity ID
- Provide event replay functionality
- Handle authentication and authorization
- Generate API documentation

**Technology Stack**:
- ASP.NET Core 8 Web API
- Entity Framework Core for data access
- Swagger/OpenAPI for documentation
- JWT authentication middleware

### Shared Library

**Purpose**: Common models, utilities, and configuration shared across services.

**Key Components**:
- Event models and schemas
- Kafka configuration
- Database context and migrations
- Serialization utilities
- Logging configuration

## Data Flow

1. **Event Generation**: Microservices publish events to Kafka topics
2. **Event Ingestion**: EventIngestor consumes events and stores them
3. **Event Storage**: Events are persisted in PostgreSQL with immutable guarantees
4. **Event Querying**: AuditApi provides REST endpoints for event access
5. **Event Consumption**: Tools and services query events via the API

## Event Schema

All events follow a consistent schema:

```json
{
  "eventId": "uuid",
  "eventType": "string",
  "aggregateId": "string",
  "aggregateType": "string",
  "timestamp": "datetime",
  "version": "integer",
  "payload": "jsonb",
  "metadata": {
    "correlationId": "string",
    "causationId": "string",
    "userId": "string",
    "source": "string"
  }
}
```

## Database Schema

### events table

```sql
CREATE TABLE events (
    id BIGSERIAL PRIMARY KEY,
    event_id UUID NOT NULL UNIQUE,
    event_type VARCHAR(255) NOT NULL,
    aggregate_id VARCHAR(255) NOT NULL,
    aggregate_type VARCHAR(255) NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    version INTEGER NOT NULL,
    payload JSONB NOT NULL,
    metadata JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_events_aggregate ON events(aggregate_type, aggregate_id);
CREATE INDEX idx_events_type ON events(event_type);
CREATE INDEX idx_events_timestamp ON events(timestamp);
CREATE INDEX idx_events_payload ON events USING GIN(payload);
```

## Security Considerations

- **Authentication**: JWT tokens for API access
- **Authorization**: Role-based access control
- **Data Encryption**: Sensitive fields encrypted at rest
- **Audit Logging**: All API access logged
- **Network Security**: TLS for all communications

## Scalability & Performance

- **Kafka Partitioning**: Events partitioned by aggregate ID
- **Database Partitioning**: Table partitioned by timestamp
- **Read Replicas**: Separate read replicas for query workloads
- **Caching**: Redis cache for frequently accessed events
- **Connection Pooling**: Optimized database connections

## Monitoring & Observability

- **Metrics**: Prometheus metrics for all services
- **Logging**: Structured logging with correlation IDs
- **Tracing**: Distributed tracing with OpenTelemetry
- **Health Checks**: Kubernetes-ready health endpoints
- **Alerting**: Alerts for critical system events

## Deployment

The system is designed for containerized deployment on Kubernetes:

- **Docker Images**: Multi-stage builds for optimal size
- **Helm Charts**: Parameterized deployment configurations
- **CI/CD**: GitHub Actions for automated builds and deployments
- **Infrastructure**: Terraform for cloud resource management

## Design Decisions

All major architectural decisions are documented in [DECISIONS.md](DECISIONS.md) with full context, alternatives considered, and rationale.
