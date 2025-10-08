# TodoApp Infrastructure Configuration

## Overview

This project supports **DUAL INFRASTRUCTURE** modes:

- **Azure Cloud Services** (Production - Azure Functions)
- **Local Development Services** (Development - Web API)

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     TodoApp Solution                         │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌───────────────────┐        ┌──────────────────┐          │
│  │  TodoApp.API      │        │ TodoApp.Functions │          │
│  │  (Web API)        │        │ (Azure Functions) │          │
│  └─────────┬─────────┘        └────────┬─────────┘          │
│            │                           │                     │
│            │                           │                     │
│  ┌─────────▼─────────────────────────▼─────────┐           │
│  │     TodoApp.Infrastructure                   │           │
│  │  (Shared with Provider Pattern)              │           │
│  └──────────────────────────────────────────────┘           │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Services Mapping

| Service Type      | Azure (Functions)      | Local (Web API)     |
| ----------------- | ---------------------- | ------------------- |
| **Database**      | PostgreSQL / Cosmos DB | PostgreSQL (Docker) |
| **Storage**       | Azure Blob Storage     | MinIO S3 (Docker)   |
| **Message Queue** | Azure Service Bus      | RabbitMQ (Docker)   |

## Configuration

### 1. Web API (Local Development)

**File**: `src/TodoApp.API/appsettings.Development.json`

```json
{
  "DatabaseProvider": "PostgreSQL",
  "StorageProvider": "MinIO",
  "MessageQueueProvider": "RabbitMQ",

  "ConnectionStrings": {
    "TodoDb": "Host=localhost;Port=5432;Database=tododb;Username=postgres;Password=postgres"
  },

  "MinioSettings": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "todo-imports",
    "UseSSL": false
  },

  "RabbitMQSettings": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "QueueName": "import-csv-queue",
    "ExchangeName": "todo-exchange",
    "RoutingKey": "todo.import"
  }
}
```

### 2. Azure Functions (Production)

**File**: `src/TodoApp.Functions/appsettings.json`

```json
{
  "DatabaseProvider": "CosmosDB",

  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://...;AccountKey=...;",
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;",
    "ServiceBus": "Endpoint=sb://...servicebus.windows.net/;SharedAccessKeyName=...;"
  }
}
```

> **Note**: Azure Functions ALWAYS use Azure services (hardcoded in Program.cs)

## Docker Compose Services

```bash
# Start all local services
docker-compose up -d

# Services will be available at:
# - PostgreSQL: localhost:5432
# - MinIO Console: http://localhost:9001 (UI)
# - MinIO API: http://localhost:9000
# - RabbitMQ Console: http://localhost:15672 (UI)
# - RabbitMQ AMQP: localhost:5672
```

### Access Credentials

**PostgreSQL**

- Username: `postgres`
- Password: `postgres`
- Database: `tododb`

**MinIO**

- Access Key: `minioadmin`
- Secret Key: `minioadmin`
- Console: http://localhost:9001

**RabbitMQ**

- Username: `guest`
- Password: `guest`
- Management UI: http://localhost:15672

## Running the Projects

### Web API (Local Development)

```bash
cd src/TodoApp.API

# Start Docker services first
docker-compose up -d

# Run the API
dotnet run
```

### Azure Functions (Production)

```bash
cd src/TodoApp.Functions

# Functions will use Azure services (no Docker needed)
func start
# or
dotnet run
```

## Provider Pattern Implementation

The infrastructure uses the **Strategy Pattern** to switch between providers:

### DependencyInjection.cs

```csharp
// Storage Services
services.AddStorageServices(configuration, StorageProvider.MinIO);
services.AddStorageServices(configuration, StorageProvider.AzureBlob);

// Message Queue Services
services.AddMessageQueueServices(configuration, MessageQueueProvider.RabbitMQ);
services.AddMessageQueueServices(configuration, MessageQueueProvider.AzureServiceBus);
```

### Service Implementations

| Interface            | Azure Implementation                    | Local Implementation          |
| -------------------- | --------------------------------------- | ----------------------------- |
| `IBlobService`       | `BlobService` (Azure Blob)              | `MinioStorageService` (MinIO) |
| `IServiceBusService` | `ServiceBusService` (Azure Service Bus) | `RabbitMQService` (RabbitMQ)  |

## Switching Between Providers

To switch providers, simply change the configuration in `appsettings.json`:

```json
{
  "StorageProvider": "MinIO", // or "AzureBlob"
  "MessageQueueProvider": "RabbitMQ" // or "AzureServiceBus"
}
```

No code changes required! ✨

## Benefits

✅ **Consistent Interface**: Same `IBlobService` and `IServiceBusService` interfaces  
✅ **Easy Testing**: Use local services for development  
✅ **Cost Effective**: No Azure charges during development  
✅ **Production Ready**: Azure Functions use production services  
✅ **Flexible**: Switch providers via configuration

## Troubleshooting

### Web API can't connect to Docker services

```bash
# Check if services are running
docker-compose ps

# Check logs
docker-compose logs postgres
docker-compose logs minio
docker-compose logs rabbitmq

# Restart services
docker-compose restart
```

### MinIO bucket not created

MinIO buckets are created automatically on first upload. You can also create manually:

```bash
# Access MinIO console at http://localhost:9001
# Login: minioadmin / minioadmin
# Create bucket: todo-imports
```

### RabbitMQ queue not created

RabbitMQ queues and exchanges are created automatically when the service starts. Check the management UI:

```
http://localhost:15672
Login: guest / guest
```

## Architecture Diagram

```
                    ┌─────────────────┐
                    │  Client Request │
                    └────────┬────────┘
                             │
              ┌──────────────┴──────────────┐
              │                             │
              ▼                             ▼
    ┌──────────────────┐        ┌──────────────────┐
    │  TodoApp.API     │        │ TodoApp.Functions │
    │  (Development)   │        │   (Production)    │
    └────────┬─────────┘        └────────┬──────────┘
             │                           │
             │  Configure Provider       │  Hardcoded Azure
             │                           │
    ┌────────▼─────────┐        ┌────────▼──────────┐
    │  MinIO Storage   │        │ Azure Blob Storage │
    │  RabbitMQ Queue  │        │ Azure Service Bus  │
    │  PostgreSQL DB   │        │ CosmosDB / Postgres│
    └──────────────────┘        └────────────────────┘
           Docker                      Azure Cloud
```

## Summary

- **Web API**: Uses local Docker services (MinIO, RabbitMQ, PostgreSQL)
- **Azure Functions**: Uses Azure cloud services (Blob, Service Bus, CosmosDB)
- **Shared Infrastructure**: Same code, different providers via configuration
- **Easy Development**: No Azure costs during development
- **Production Ready**: Azure Functions ready for production deployment
