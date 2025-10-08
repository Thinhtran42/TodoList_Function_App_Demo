# Infrastructure Services Architecture

## Visual Structure

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     TodoApp.Infrastructure.Services                      │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                ┌───────────────────┼───────────────────┐
                │                   │                   │
                ▼                   ▼                   ▼
    ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
    │   Persistence    │  │     Storage      │  │  MessageBroker   │
    └──────────────────┘  └──────────────────┘  └──────────────────┘
            │                      │                      │
            │                      │                      │
    ┌───────┴────────┐    ┌───────┴────────┐    ┌───────┴────────┐
    │                │    │                │    │                │
    ▼                ▼    ▼                ▼    ▼                ▼
┌─────────┐  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐
│   JWT   │  │   CSV   │ │  Azure  │ │  MinIO  │ │ Service │ │ RabbitMQ│
│ Service │  │ Export  │ │  Blob   │ │   S3    │ │   Bus   │ │         │
└─────────┘  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘
```

## Service Categorization

```
📁 Services/
│
├── 📂 Persistence/
│   │   🎯 Purpose: Data & Authentication
│   │
│   ├── 🔑 JwtService.cs
│   │   ├── Generate JWT tokens
│   │   ├── Validate tokens
│   │   ├── Refresh tokens
│   │   └── Revoke tokens
│   │
│   └── 📊 CsvExportService.cs
│       ├── Export Todo items to CSV
│       ├── Upload to blob storage
│       └── Generate SAS URLs
│
├── 📂 Storage/
│   │   🎯 Purpose: File Storage (Multi-Provider)
│   │
│   ├── ☁️  BlobService.cs (Azure - Production)
│   │   ├── Upload files to Azure Blob
│   │   ├── Download files from Azure Blob
│   │   └── Manage containers
│   │
│   └── 🐳 MinioStorageService.cs (Local - Development)
│       ├── Upload files to MinIO
│       ├── Download files from MinIO
│       └── Manage buckets
│
└── 📂 MessageBroker/
    │   🎯 Purpose: Async Messaging (Multi-Provider)
    │
    ├── ☁️  ServiceBusService.cs (Azure - Production)
    │   ├── Send messages to Azure Service Bus
    │   ├── Queue management
    │   └── Message serialization
    │
    └── 🐰 RabbitMQService.cs (Local - Development)
        ├── Send messages to RabbitMQ
        ├── Exchange & queue declaration
        └── Connection management
```

## Provider Pattern Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      Application Request                         │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Dependency Injection Container                 │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│              Read Configuration (appsettings.json)               │
│   StorageProvider: "MinIO" | "AzureBlob"                        │
│   MessageQueueProvider: "RabbitMQ" | "AzureServiceBus"         │
└────────────────────────────┬────────────────────────────────────┘
                             │
              ┌──────────────┴──────────────┐
              │                             │
              ▼                             ▼
    ┌──────────────────┐          ┌──────────────────┐
    │  Local Services  │          │  Azure Services  │
    │   (Development)  │          │   (Production)   │
    └──────────────────┘          └──────────────────┘
              │                             │
    ┌─────────┴─────────┐       ┌──────────┴──────────┐
    │                   │       │                     │
    ▼                   ▼       ▼                     ▼
┌─────────┐      ┌──────────┐ ┌──────────┐    ┌──────────┐
│  MinIO  │      │ RabbitMQ │ │Azure Blob│    │Service   │
│Storage  │      │  Queue   │ │ Storage  │    │   Bus    │
└─────────┘      └──────────┘ └──────────┘    └──────────┘
    │                   │           │               │
    └─────────┬─────────┘           └───────┬───────┘
              │                             │
              ▼                             ▼
    ┌──────────────────┐          ┌──────────────────┐
    │ IBlobService     │          │ IBlobService     │
    │IServiceBusService│          │IServiceBusService│
    └──────────────────┘          └──────────────────┘
              │                             │
              └──────────────┬──────────────┘
                             │
                             ▼
              ┌──────────────────────────┐
              │   Application Logic      │
              │  (Same interface used)   │
              └──────────────────────────┘
```

## Service Interaction Flow

### Example: CSV Import Flow

```
┌──────────┐
│  Client  │
└────┬─────┘
     │ 1. POST /api/import
     ▼
┌──────────────────┐
│ ImportController │
└────┬─────────────┘
     │ 2. Upload CSV
     ▼
┌──────────────────┐
│  IBlobService    │ ◄─── Interface (abstraction)
└────┬─────────────┘
     │
     ├─── Development ───┐        ┌─── Production ───┐
     │                   │        │                  │
     ▼                   ▼        ▼                  ▼
┌─────────────┐   ┌──────────┐  ┌──────────┐  ┌──────────┐
│MinioStorage │   │ MinIO    │  │BlobService│ │Azure Blob│
│  Service    │──▶│  Docker  │  │          │─▶│ Storage  │
└─────────────┘   └──────────┘  └──────────┘  └──────────┘
     │                                 │
     │ 3. Get blob URL                │
     ▼                                 ▼
┌──────────────────┐          ┌──────────────────┐
│ImportController  │          │ImportController  │
└────┬─────────────┘          └────┬─────────────┘
     │ 4. Queue message            │
     ▼                             ▼
┌──────────────────┐          ┌──────────────────┐
│IServiceBusService│          │IServiceBusService│
└────┬─────────────┘          └────┬─────────────┘
     │                             │
     ├─── Development ───┐         ├─── Production ───┐
     │                   │         │                  │
     ▼                   ▼         ▼                  ▼
┌─────────────┐   ┌──────────┐  ┌──────────┐  ┌──────────┐
│ RabbitMQ    │   │RabbitMQ  │  │ServiceBus│  │Azure SB  │
│  Service    │──▶│  Docker  │  │ Service  │─▶│          │
└─────────────┘   └──────────┘  └──────────┘  └──────────┘
     │                                 │
     │ 5. Message queued              │
     ▼                                 ▼
┌──────────────────────────────────────────────┐
│         Azure Functions Worker               │
│       (Processes import async)               │
└──────────────────────────────────────────────┘
```

## Interface Contracts

### IBlobService

```csharp
┌──────────────────────────────────────────┐
│          <<interface>>                   │
│          IBlobService                    │
├──────────────────────────────────────────┤
│ + UploadCsvAsync()   : Task<string>     │
│ + DownloadCsvAsync() : Task<string>     │
└──────────────────────────────────────────┘
         △                    △
         │                    │
         │                    │
┌────────┴────────┐  ┌────────┴────────┐
│  BlobService    │  │MinioStorage     │
│  (Azure Prod)   │  │Service (Local)  │
└─────────────────┘  └─────────────────┘
```

### IServiceBusService

```csharp
┌──────────────────────────────────────────┐
│          <<interface>>                   │
│       IServiceBusService                 │
├──────────────────────────────────────────┤
│ + SendImportMessageAsync() : Task<str>  │
└──────────────────────────────────────────┘
         △                    △
         │                    │
         │                    │
┌────────┴────────┐  ┌────────┴────────┐
│  ServiceBus     │  │  RabbitMQ       │
│  Service (AZ)   │  │  Service (Local)│
└─────────────────┘  └─────────────────┘
```

## Configuration Hierarchy

```
appsettings.json
├── DatabaseProvider
│   ├── "PostgreSQL" → PostgreSQL (Docker/Azure)
│   └── "CosmosDB"   → Cosmos DB (Azure)
│
├── StorageProvider
│   ├── "MinIO"      → MinioStorageService → Docker MinIO
│   └── "AzureBlob"  → BlobService → Azure Blob Storage
│
└── MessageQueueProvider
    ├── "RabbitMQ"        → RabbitMQService → Docker RabbitMQ
    └── "AzureServiceBus" → ServiceBusService → Azure Service Bus
```

## Deployment Scenarios

```
┌────────────────────────────────────────────────────────────┐
│                    Development (Local)                      │
├────────────────────────────────────────────────────────────┤
│  App: TodoApp.API                                          │
│  Database: PostgreSQL (Docker)                             │
│  Storage: MinIO (Docker)                                   │
│  Queue: RabbitMQ (Docker)                                  │
│  Cost: $0                                                  │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│                 Production (Azure Cloud)                    │
├────────────────────────────────────────────────────────────┤
│  App: TodoApp.Functions (Azure Functions)                  │
│  Database: Cosmos DB / Azure PostgreSQL                    │
│  Storage: Azure Blob Storage                               │
│  Queue: Azure Service Bus                                  │
│  Cost: Pay-as-you-go                                       │
└────────────────────────────────────────────────────────────┘
```

## Benefits Summary

```
┌─────────────────────────────────────────────────────────┐
│                  Refactoring Benefits                    │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ✅ Clear Separation of Concerns                        │
│     Services grouped by responsibility                  │
│                                                          │
│  ✅ Easy Navigation                                      │
│     Find services by category quickly                   │
│                                                          │
│  ✅ Scalable Architecture                               │
│     Add new services/providers easily                   │
│                                                          │
│  ✅ Multi-Environment Support                           │
│     Same code, different providers                      │
│                                                          │
│  ✅ Cost Effective                                       │
│     Free local development                              │
│                                                          │
│  ✅ Testable                                             │
│     Mock interfaces easily                              │
│                                                          │
│  ✅ Production Ready                                     │
│     Azure Functions use production services             │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

**Architecture Design**: Clean Architecture + Provider Pattern  
**Last Updated**: October 8, 2025
