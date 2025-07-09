# API Reference

## Overview

The Audit API provides RESTful endpoints for querying and analyzing events in the audit trail system. All endpoints require authentication and follow consistent response patterns.

## Base URL

- **Development**: `http://localhost:5000`
- **Production**: `https://audit-api.your-domain.com`

## Authentication

All endpoints require JWT authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Common Response Format

All responses follow this structure:

```json
{
  "success": true,
  "data": {},
  "errors": [],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalCount": 1000,
    "totalPages": 20
  }
}
```

## Error Responses

Error responses include detailed information:

```json
{
  "success": false,
  "data": null,
  "errors": [
    {
      "code": "VALIDATION_ERROR",
      "message": "Invalid date format",
      "field": "startDate"
    }
  ]
}
```

## Endpoints

### Health Check

#### `GET /health`

Returns the health status of the API.

**Response:**
```json
{
  "success": true,
  "data": {
    "status": "healthy",
    "timestamp": "2025-01-08T10:30:00Z",
    "version": "1.0.0"
  }
}
```

---

### List Events

#### `GET /api/events`

Retrieves a paginated list of events with optional filtering.

**Query Parameters:**

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `page` | integer | Page number (default: 1) | `?page=2` |
| `pageSize` | integer | Items per page (default: 50, max: 1000) | `?pageSize=100` |
| `startDate` | datetime | Filter events after this date | `?startDate=2025-01-01T00:00:00Z` |
| `endDate` | datetime | Filter events before this date | `?endDate=2025-01-08T23:59:59Z` |
| `eventType` | string | Filter by event type | `?eventType=OrderCreated` |
| `aggregateId` | string | Filter by aggregate ID | `?aggregateId=order-123` |
| `aggregateType` | string | Filter by aggregate type | `?aggregateType=Order` |

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/events?eventType=OrderCreated&pageSize=10" \
  -H "Authorization: Bearer <token>"
```

**Example Response:**
```json
{
  "success": true,
  "data": [
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
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1
  }
}
```

---

### Get Event by ID

#### `GET /api/events/{eventId}`

Retrieves a specific event by its ID.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `eventId` | UUID | The unique event identifier |

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/events/550e8400-e29b-41d4-a716-446655440000" \
  -H "Authorization: Bearer <token>"
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "eventId": "550e8400-e29b-41d4-a716-446655440000",
    "eventType": "OrderCreated",
    "aggregateId": "order-123",
    "aggregateType": "Order",
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
}
```

---

### Get Events by Aggregate

#### `GET /api/aggregates/{aggregateType}/{aggregateId}/events`

Retrieves all events for a specific aggregate in chronological order.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `aggregateType` | string | The type of aggregate (e.g., "Order") |
| `aggregateId` | string | The unique aggregate identifier |

**Query Parameters:**

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `upToVersion` | integer | Include events up to this version | `?upToVersion=5` |
| `fromVersion` | integer | Include events from this version | `?fromVersion=2` |

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/aggregates/Order/order-123/events" \
  -H "Authorization: Bearer <token>"
```

**Example Response:**
```json
{
  "success": true,
  "data": [
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
        "amount": 99.99
      }
    },
    {
      "eventId": "550e8400-e29b-41d4-a716-446655440001",
      "eventType": "OrderPaid",
      "aggregateId": "order-123",
      "aggregateType": "Order",
      "timestamp": "2025-01-08T10:35:00Z",
      "version": 2,
      "payload": {
        "orderId": "order-123",
        "paymentId": "payment-789",
        "amount": 99.99
      }
    }
  ]
}
```

---

### Event Statistics

#### `GET /api/events/statistics`

Returns statistics about events in the system.

**Query Parameters:**

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `startDate` | datetime | Statistics from this date | `?startDate=2025-01-01T00:00:00Z` |
| `endDate` | datetime | Statistics to this date | `?endDate=2025-01-08T23:59:59Z` |
| `groupBy` | string | Group statistics by field | `?groupBy=eventType` |

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/events/statistics?groupBy=eventType" \
  -H "Authorization: Bearer <token>"
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "totalEvents": 10000,
    "dateRange": {
      "start": "2025-01-01T00:00:00Z",
      "end": "2025-01-08T23:59:59Z"
    },
    "groupedStatistics": [
      {
        "key": "OrderCreated",
        "count": 2500,
        "percentage": 25.0
      },
      {
        "key": "OrderPaid",
        "count": 2000,
        "percentage": 20.0
      },
      {
        "key": "OrderShipped",
        "count": 1800,
        "percentage": 18.0
      }
    ]
  }
}
```

---

### Replay Events

#### `POST /api/events/replay`

Replays events to reconstruct state or trigger downstream processing.

**Request Body:**
```json
{
  "filters": {
    "startDate": "2025-01-01T00:00:00Z",
    "endDate": "2025-01-08T23:59:59Z",
    "eventTypes": ["OrderCreated", "OrderPaid"],
    "aggregateIds": ["order-123", "order-456"]
  },
  "destination": {
    "type": "webhook",
    "url": "https://your-service.com/webhook/replay"
  }
}
```

**Example Request:**
```bash
curl -X POST "http://localhost:5000/api/events/replay" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "filters": {
      "startDate": "2025-01-01T00:00:00Z",
      "endDate": "2025-01-08T23:59:59Z",
      "eventTypes": ["OrderCreated"]
    },
    "destination": {
      "type": "webhook",
      "url": "https://your-service.com/webhook/replay"
    }
  }'
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "replayId": "replay-550e8400-e29b-41d4-a716-446655440000",
    "status": "started",
    "estimatedEventCount": 1000,
    "createdAt": "2025-01-08T10:30:00Z"
  }
}
```

---

### Get Replay Status

#### `GET /api/events/replay/{replayId}`

Retrieves the status of a replay operation.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `replayId` | UUID | The unique replay identifier |

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/events/replay/replay-550e8400-e29b-41d4-a716-446655440000" \
  -H "Authorization: Bearer <token>"
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "replayId": "replay-550e8400-e29b-41d4-a716-446655440000",
    "status": "completed",
    "progress": {
      "totalEvents": 1000,
      "processedEvents": 1000,
      "percentageComplete": 100
    },
    "startedAt": "2025-01-08T10:30:00Z",
    "completedAt": "2025-01-08T10:35:00Z"
  }
}
```

---

## Error Codes

| Code | Description |
|------|-------------|
| `VALIDATION_ERROR` | Request validation failed |
| `UNAUTHORIZED` | Authentication required |
| `FORBIDDEN` | Insufficient permissions |
| `NOT_FOUND` | Requested resource not found |
| `RATE_LIMIT_EXCEEDED` | Too many requests |
| `INTERNAL_ERROR` | Server error |

## Rate Limiting

API endpoints are rate-limited to prevent abuse:

- **Standard endpoints**: 1000 requests per hour per user
- **Replay endpoints**: 10 requests per hour per user

Rate limit headers are included in responses:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1609459200
```

## Pagination

All list endpoints support pagination:

- Maximum page size: 1000 items
- Default page size: 50 items
- Use `page` and `pageSize` query parameters

## Filtering

Common filter parameters:

- **Date ranges**: Use ISO 8601 format (YYYY-MM-DDTHH:MM:SSZ)
- **Event types**: Exact string matching
- **Aggregate IDs**: Exact string matching
- **Multiple values**: Use comma-separated values

## OpenAPI Specification

The complete OpenAPI specification is available at `/swagger` when the API is running.
