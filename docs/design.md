# Core Banking Event Store Design

## Overview

This document outlines the design of a core banking system built using Event Sourcing and CQRS patterns. The system is split into two main packages:

1. EventStore - A generic event sourcing framework
2. CoreBanking - The banking domain implementation

## Architecture

### Solution Structure
```
solution/
├── src/
│   ├── EventStore/                     # Generic event sourcing framework
│   │   ├── EventStore.Api/             # Web API
│   │   ├── EventStore.Application/     # Use cases, application logic, DTOs, commands, queries and handlers
│   │   ├── EventStore.Domain/          # Domain entities, value objects, events, interfaces, and aggregates
│   |   ├── EventStore.Infrastructure/  # Implementation details
│   │   ├── EventStore.Integration/     # Integration events or specific triggers.  In this case Azure Blob Storage
│   |   └── EventStore.Tests/           # Integration and unit tests
│   │
│   ├── CoreBanking/                    # Banking domain implementation
│   |   ├── CoreBanking.Domain/         # Domain entities, value objects, events, interfaces, and aggregates
│   |   ├── CoreBanking.Application/    # Use cases, application logic, DTOs, commands, queries and handlers
│   |   ├── CoreBanking.Infrastructure/ # Implementation details
│   |   ├── CoreBanking.Api/            # Web API
│   |   └── CoreBanking.Tests/          # Integration and unit tests
│   |
│   └── infrastructure/                 # Banking domain implementation
│       ├── workstation/                # Workstation setup
│       └── azure/                      # Azure setup
│
└── docker/
    └── docker-compose.yml
```

## Technical Choices

### Core Technologies
- **.NET 9**: Latest LTS version for robust enterprise development
- **C#**: Strong typing and modern language features
- **ASP.NET Core**: Web API framework
- **Azure Blob Storage**: Event store persistence
- **Docker**: Containerization for local development
- **Azurite**: Local Azure Storage emulator

### Design Patterns
- **Event Sourcing**: All state changes are stored as a sequence of events
- **CQRS**: Separate command and query responsibilities
- **Domain-Driven Design**: Rich domain model with aggregates
- **Repository Pattern**: Abstraction over event store implementation

## EventStore

EventStore is a generic event sourcing framework that provides a set of interfaces and implementations for event sourcing. It is designed to be used as a base for domain specific event stores. 

EventStore has a control plane and a data plane.  The control plane is responsible for managing the event store:

- Health checks,
- Creating topics,
- Managing topic configuration,
- Deleting topics, and 
- Querying topics.

The data plane is responsible for accessing topics:

- Writing an event to a topic, and
- Registering a callback to a topic.

All access to the event store is through the API.  The API is a RESTful API that is designed to be used by the Core Banking application.

### Control plane

The control plane is responsible for managing the event store.  It is a RESTful API that is designed to be used by the Core Banking application.

#### Health Checks

The control plane has a health check endpoint that can be used to check the health of the event store.  It is a simple endpoint that returns a 200 OK response if the event store is healthy.

The health check endpoint is defined as follows:

```
GET /api/v1/health
```

The health check endpoint returns a 200 OK response if the event store is healthy.

#### Creating a topic

The control plane has a command endpoint for creating a topic.  The endpoint is defined as follows:

```
POST /api/v1/topics
```

The request body is a JSON object that contains the topic configuration.  The topic configuration is defined as follows:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "CreateTopicRequest",
  "type": "object",
  "required": ["topicName", "description"],
  "properties": {
    "topicName": {
      "type": "string",
    },
    "description": {
      "type": "string"
    },
    "eventSchemas": {
      "type": "array",
      "description": "Array of event schemas supported by this topic",
      "items": {
        "type": "object",
        "required": ["eventType", "schema"],
        "properties": {
          "eventType": {
            "type": "string",
            "description": "The type of event this schema validates"
          },
          "schema": {
            "type": "object",
            "description": "JSON Schema definition for this event type"
          }
        }
      },
      "minItems": 1
    }
  },
  "additionalProperties": false
}```


The response includes:
- The topic name and description
- The version number; defaults to 1 should this be a new topic.
- Creation timestamp

The response will include the created topic's details and a 201 Created status code.

```json
{
    "$schema": "http://json-schema.org/draft-07/schema#",
    "title": "CreateTopicResponse",
    "type": "object",
    "properties": {
        "topicName": {
            "type": "string"
        },
        "description": {
            "type": "string"
        },
        "version": {
            "type": "number"
        },
        "createdAt": {
            "type": "string",
            "format": "date-time"
        }
    }
}
```

The design of this endpoint is to be built using CQRS.  The list of topics is an in-memory read-model that is used to represents the topics in the event store.  This structure is updated through an event handler that is triggered from the data plane whenever a topic create command is executed.  The management events are placed into their own topic called "_configuration" in the event store.

The read model is updated through the

```
POST /api/v1/event-handler
```



### Data plane

The data plane is responsible for accessing topics either writing events to or reading events from.  It is a RESTful API that is designed to be used by the Core Banking application.

## CoreBanking

Core Banking is a domain specific implementation of a core banking lean core ledger.  It is built on top of the EventStore framework and provides a set of domain specific interfaces and implementations.


## Storage Strategy

### Event Storage
- Events are stored in Azure Blob Storage
- Each topic has its own container
- Events are stored as JSON blobs
- Version numbers are used for optimistic concurrency
- Events include metadata for type discrimination

### Local Development
- Azurite emulator for local Azure Storage
- Docker Compose for container orchestration
- In-memory event store option for testing

## Security Considerations

### Authentication & Authorization
- JWT-based authentication
- Role-based access control
- API key authentication for service-to-service communication

### Data Protection
- All sensitive data encrypted at rest
- TLS for all communications
- Audit logging for all operations

## Performance Considerations

### Optimizations
- Event serialization caching
- Read model projections for efficient querying
- Snapshot support for large aggregates
- Batch processing for high-volume operations

### Scalability
- Horizontal scaling of API servers
- Partitioned event storage
- Caching layer for read models
- Async processing for non-critical operations

## Monitoring & Observability

### Logging
- Structured logging with correlation IDs
- Event sourcing audit trail
- Performance metrics
- Error tracking

### Health Checks
- API health endpoints
- Storage connectivity checks
- Dependency health monitoring

## Development Workflow

### Local Development
1. Docker Compose for local infrastructure
2. Azurite for Azure Storage emulation
3. Hot reload for rapid development
4. Integrated testing environment

### Testing Strategy
- Unit tests for domain logic - xUnit
- Integration tests for event store - xUnit
- End-to-end tests for API - xUnit
- Performance testing suite - xUnit

Assertions are written using the FluentAssertions library.

## Deployment

### Infrastructure
- Azure App Service for API
- Azure Blob Storage for event store
- Azure Key Vault for secrets
- Azure Monitor for observability

### CI/CD
- GitHub Actions for automation
- Automated testing
- Infrastructure as Code
- Blue-green deployment

## Future Considerations

### Potential Enhancements
1. Event versioning and schema evolution
2. Read model projections
3. Snapshot support
4. Event replay capabilities
5. Multi-region support
6. Enhanced monitoring
7. Performance optimizations

### Scalability Paths
1. Horizontal scaling of API
2. Partitioned event storage
3. Caching strategies
4. Async processing
5. Read model optimization 

## API

The API uses URL-based versioning with the following pattern:

```
/api/v{version}/{resource}
```

For example:
- `/api/v1/health` - Health check endpoint
- `/api/v1/topics` - Topic management
- `/api/v1/events` - Event operations

Versioning Rules:
1. Major version changes (v1 -> v2) indicate breaking changes
2. Minor version changes are handled through new endpoints or query parameters
3. Patch version changes are transparent to API consumers

Version Lifecycle:
- Each major version is supported for at least 12 months after the release of the next major version
- Deprecation notices are provided 6 months before version retirement
- Multiple versions can be active simultaneously during transition periods

Version Headers:
- `api-deprecated` header indicates when a version will be retired

Example Versioned Endpoints:
```
# Control Plane
GET    /api/v1/health
POST   /api/v1/topics
GET    /api/v1/topics/{topicId}
DELETE /api/v1/topics/{topicId}

# Data Plane
POST   /api/v1/topics/{topicId}/events
GET    /api/v1/topics/{topicId}/events
GET    /api/v1/topics/{topicId}/events/{eventId}
```

src/
└── EventStore/
    └── EventStore/
        └── Health/
            ├── Models/     # For DTOs
            └── Services/   # For health check services

