# EventStore: A Modern Event-Driven Banking Platform 🏦

Welcome to EventStore, an experimental lean core banking platform built using event-driven architecture and CQRS (Command Query Responsibility Segregation) principles. This platform is designed to handle banking operations with reliability, scalability, and maintainability in mind.

## 🌟 Features

- Event-driven architecture for decoupled and scalable operations
- CQRS pattern implementation for optimized read and write operations
- Azure Blob Storage integration for durable event storage
- Topic-based event organization with JSON schema validation
- Health monitoring endpoints with detailed diagnostics
- Continuous Integration with automated testing

## 🏗 Technical Architecture

### Core Components

- **Domain Layer**: Contains the core business logic, entities, and domain events
- **Application Layer**: Handles command and event processing
- **Infrastructure Layer**: Provides implementations for external services and storage
- **API Layer**: RESTful endpoints for system interaction

### Key Technologies

- **.NET 8**: Latest .NET runtime for optimal performance
- **Azure Blob Storage**: Durable event storage
- **JSON Schema**: Event validation and structure definition
- **xUnit**: Comprehensive test coverage
- **FluentAssertions**: Expressive test assertions
- **GitHub Actions**: Automated CI/CD pipeline

## 🚀 Getting Started

### Prerequisites

- Docker Desktop
- Docker Compose
- Git

### Local Setup

1. Clone the repository:
   ```bash
   git clone [repository-url]
   cd eventstore
   ```

2. Build and start the services using Docker Compose:
   ```bash
   cd ./docker
   docker compose -f docker/docker-compose.yml up --build
   ```
   This will:
   - Build and start the EventStore API (available at http://localhost:5050)
   - Start Azurite for local Azure Storage emulation
   - Set up the necessary networking between services
   - Configure health checks for service monitoring

3. Verify the setup:
   ```bash
   curl http://localhost:5050/api/v1/health
   ```
   You should see a health check response indicating the system status.

4. To stop the services:
   ```bash
   docker compose -f docker/docker-compose.yml down
   ```

### Development

For local development and running tests:

1. Install the .NET 8 SDK
2. Run the tests:
   ```bash
   dotnet test
   ```

### Continuous Integration

The project uses GitHub Actions for continuous integration. On every push and pull request:
- Builds the solution
- Runs all tests with Azurite for Azure Storage emulation
- Validates code quality

You can view the CI status in the GitHub Actions tab of the repository.

## 🔌 API Endpoints

### Health Check
```http
GET /api/v1/health
```
Returns the health status of the system, including:
- Blob storage connectivity
- System resources utilization
- Overall service health

### Topic Management
```http
POST /api/v1/topics
```
Creates a new event topic with:
- Topic name and description
- Event schemas for validation
- Version control

## 🤝 Contributing

We welcome contributions! Please feel free to submit pull requests, create issues, or suggest improvements.

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.
