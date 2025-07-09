# Order Event Tracking & Audit Trail System

## Overview

This project implements a distributed, event-driven backend for capturing, storing, and exposing an immutable history of all business events (such as order creation, inventory updates, payment changes, etc.) across our platform. The system ingests events from microservices via Kafka, persists them in an append-only event store (PostgreSQL), and provides APIs for querying, analysis, and replay.

## Key Goals

* **Traceability:** Always know *what happened, when, and why*
* **Compliance & Auditing:** Meet legal/business requirements for data history
* **Debugging:** Reconstruct workflows and state at any time
* **Analytics:** Enable event-based analytics and anomaly detection

## Architecture

The system consists of two main microservices:

- **EventIngestor**: Kafka consumer that ingests and persists events
- **AuditApi**: REST API for querying and replaying events

All design and implementation choices are documented in `/docs/DECISIONS.md`, making the reasoning behind every major choice transparent and maintainable.

## Quick Start

### Prerequisites

- Docker and Docker Compose
- .NET 8 SDK
- PostgreSQL 15+
- Kafka 3.0+

### Local Development

1. Clone the repository:
   ```bash
   git clone https://github.com/your-org/tcg-order-audit-trail.git
   cd tcg-order-audit-trail
   ```

2. Copy environment configuration:
   ```bash
   cp .env.example .env
   ```

3. Start local infrastructure:
   ```bash
   docker-compose up -d
   ```

4. Run database migrations:
   ```bash
   dotnet run --project migrations
   ```

5. Start the services:
   ```bash
   # Terminal 1 - Event Ingestor
   dotnet run --project src/EventIngestor
   
   # Terminal 2 - Audit API
   dotnet run --project src/AuditApi
   ```

## Documentation

- [Architecture & Design](docs/ARCHITECTURE.md)
- [Decision Log](docs/DECISIONS.md)
- [API Reference](docs/API.md)
- [Kafka Guide](docs/KAFKA-CHEATSHEET.md)

## Project Structure

```
tcg-order-audit-trail/
├── src/
│   ├── EventIngestor/     # Kafka consumer service
│   ├── AuditApi/          # REST API service
│   └── Shared/            # Shared libraries and models
├── migrations/            # Database schema and migrations
├── infra/                 # Infrastructure as code
├── docs/                  # Project documentation
└── tests/                 # Automated tests
```

## Contributing

1. Review the [Architecture](docs/ARCHITECTURE.md) and [Decisions](docs/DECISIONS.md) documents
2. Follow the established patterns in the codebase
3. Add tests for new functionality
4. Update documentation when making architectural changes

## License

[MIT License](LICENSE)
