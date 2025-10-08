# TodoApp.Infrastructure Services Layer

## Folder Structure

```
Services/
├── Persistence/              # Data & authentication services
│   ├── JwtService.cs        # JWT token generation & validation
│   └── CsvExportService.cs  # CSV export to blob storage
│
├── Storage/                  # File storage services
│   ├── BlobService.cs       # Azure Blob Storage (Production)
│   └── MinioStorageService.cs # MinIO S3 Storage (Local Development)
│
└── MessageBroker/            # Message queue services
    ├── ServiceBusService.cs  # Azure Service Bus (Production)
    └── RabbitMQService.cs    # RabbitMQ (Local Development)
```

## Service Categories

### 1. Persistence Services (`Services/Persistence/`)

Services related to data persistence, authentication, and data export.

#### JwtService

- **Purpose**: JWT token management
- **Implements**: `IJwtService`
- **Responsibilities**:
  - Generate JWT access tokens
  - Generate refresh tokens
  - Validate tokens
  - Revoke tokens
- **Dependencies**:
  - `IUserRepository`
  - `JwtSettings`

#### CsvExportService

- **Purpose**: Export data to CSV and upload to storage
- **Implements**: `ICsvExportService`
- **Responsibilities**:
  - Convert Todo items to CSV format
  - Upload CSV to blob storage
  - Generate SAS URLs for secure access
- **Dependencies**:
  - Azure Blob Storage
  - CsvHelper library

---

### 2. Storage Services (`Services/Storage/`)

Services for file storage with support for both Azure and local development.

#### BlobService (Azure - Production)

- **Purpose**: Azure Blob Storage integration
- **Implements**: `IBlobService`
- **Environment**: Production (Azure)
- **Responsibilities**:
  - Upload CSV files to Azure Blob Storage
  - Download CSV files from Azure Blob Storage
  - Manage blob containers
- **Configuration**:
  ```json
  {
    "ConnectionStrings": {
      "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=..."
    }
  }
  ```

#### MinioStorageService (MinIO - Local)

- **Purpose**: MinIO S3-compatible storage
- **Implements**: `IBlobService`
- **Environment**: Local Development (Docker)
- **Responsibilities**:
  - Upload CSV files to MinIO
  - Download CSV files from MinIO
  - Manage MinIO buckets
- **Configuration**:
  ```json
  {
    "StorageProvider": "MinIO",
    "MinioSettings": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "todo-imports",
      "UseSSL": false
    }
  }
  ```

**Interface Contract**: Both services implement the same `IBlobService` interface:

```csharp
public interface IBlobService
{
    Task<string> UploadCsvAsync(byte[] csvContent, string fileName, string containerName = "imports");
    Task<string> DownloadCsvAsync(string blobUrl);
}
```

---

### 3. MessageBroker Services (`Services/MessageBroker/`)

Services for asynchronous message processing with support for both Azure and local development.

#### ServiceBusService (Azure - Production)

- **Purpose**: Azure Service Bus integration
- **Implements**: `IServiceBusService`
- **Environment**: Production (Azure)
- **Responsibilities**:
  - Send import messages to Azure Service Bus queue
  - Message serialization and metadata handling
- **Configuration**:
  ```json
  {
    "ConnectionStrings": {
      "ServiceBus": "Endpoint=sb://....servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=..."
    },
    "ServiceBusQueueName": "import-csv-queue"
  }
  ```

#### RabbitMQService (RabbitMQ - Local)

- **Purpose**: RabbitMQ message broker
- **Implements**: `IServiceBusService`
- **Environment**: Local Development (Docker)
- **Responsibilities**:
  - Send import messages to RabbitMQ queue
  - Declare exchanges, queues, and bindings
  - Connection management with auto-recovery
- **Configuration**:
  ```json
  {
    "MessageQueueProvider": "RabbitMQ",
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

**Interface Contract**: Both services implement the same `IServiceBusService` interface:

```csharp
public interface IServiceBusService
{
    Task<string> SendImportMessageAsync(ImportMessage message);
}
```

---

## Dependency Injection Registration

Services are registered in `DependencyInjection.cs` with provider-based switching:

### Storage Services

```csharp
builder.Services.AddStorageServices(configuration, StorageProvider.MinIO);
// or
builder.Services.AddStorageServices(configuration, StorageProvider.AzureBlob);
```

### Message Broker Services

```csharp
builder.Services.AddMessageQueueServices(configuration, MessageQueueProvider.RabbitMQ);
// or
builder.Services.AddMessageQueueServices(configuration, MessageQueueProvider.AzureServiceBus);
```

### Persistence Services

```csharp
services.AddScoped<IJwtService, JwtService>();
services.AddScoped<ICsvExportService, CsvExportService>();
```

---

## Provider Switching

The infrastructure supports easy switching between Azure and local services:

| Service Type      | Production (Azure)  | Development (Local)   |
| ----------------- | ------------------- | --------------------- |
| **Storage**       | `BlobService`       | `MinioStorageService` |
| **Message Queue** | `ServiceBusService` | `RabbitMQService`     |

### Configuration

**Web API (Local Development)**:

```json
{
  "StorageProvider": "MinIO",
  "MessageQueueProvider": "RabbitMQ"
}
```

**Azure Functions (Production)**:

```csharp
// Hardcoded to always use Azure services
builder.Services.AddStorageServices(config, StorageProvider.AzureBlob);
builder.Services.AddMessageQueueServices(config, MessageQueueProvider.AzureServiceBus);
```

---

## Benefits of This Structure

✅ **Clear Separation of Concerns**: Each folder represents a distinct category of services  
✅ **Easy Navigation**: Developers can quickly find services by category  
✅ **Scalability**: Easy to add new services in appropriate folders  
✅ **Testability**: Each service is isolated and mockable  
✅ **Flexibility**: Support both Azure and local development environments  
✅ **Maintainability**: Related services are grouped together

---

## Service Dependencies

### Persistence Services Dependencies

- Entity Framework Core
- JWT libraries (System.IdentityModel.Tokens.Jwt)
- Azure Storage (for CsvExportService)
- CsvHelper

### Storage Services Dependencies

- **BlobService**: Azure.Storage.Blobs
- **MinioStorageService**: Minio SDK

### MessageBroker Services Dependencies

- **ServiceBusService**: Azure.Messaging.ServiceBus
- **RabbitMQService**: RabbitMQ.Client

---

## Usage Examples

### Uploading a CSV File

```csharp
public class ImportController : ControllerBase
{
    private readonly IBlobService _blobService;

    public ImportController(IBlobService blobService)
    {
        _blobService = blobService; // Will be MinIO or Azure depending on config
    }

    public async Task<IActionResult> Upload(IFormFile file)
    {
        var bytes = await GetBytesFromFile(file);
        var blobUrl = await _blobService.UploadCsvAsync(bytes, file.FileName);
        return Ok(new { url = blobUrl });
    }
}
```

### Sending a Message

```csharp
public class ImportService
{
    private readonly IServiceBusService _messageBroker;

    public ImportService(IServiceBusService messageBroker)
    {
        _messageBroker = messageBroker; // Will be RabbitMQ or Service Bus depending on config
    }

    public async Task QueueImport(ImportMessage message)
    {
        var messageId = await _messageBroker.SendImportMessageAsync(message);
        Console.WriteLine($"Message queued: {messageId}");
    }
}
```

---

## Testing

Each service can be easily mocked for unit testing:

```csharp
[Fact]
public async Task Should_Upload_File_To_Storage()
{
    // Arrange
    var mockBlobService = new Mock<IBlobService>();
    mockBlobService
        .Setup(x => x.UploadCsvAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync("https://storage/file.csv");

    var controller = new ImportController(mockBlobService.Object);

    // Act
    var result = await controller.Upload(mockFile);

    // Assert
    mockBlobService.Verify(x => x.UploadCsvAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
}
```

---

## Migration Guide

If you need to migrate from old structure to new structure:

### Old Structure

```
Services/
├── BlobService.cs
├── ServiceBusService.cs
├── MinioStorageService.cs
├── RabbitMQService.cs
├── JwtService.cs
└── CsvExportService.cs
```

### New Structure (Current)

```
Services/
├── Persistence/
│   ├── JwtService.cs
│   └── CsvExportService.cs
├── Storage/
│   ├── BlobService.cs
│   └── MinioStorageService.cs
└── MessageBroker/
    ├── ServiceBusService.cs
    └── RabbitMQService.cs
```

### Migration Steps

1. Move files to appropriate folders
2. Update namespaces in each file
3. Update using statements in `DependencyInjection.cs`
4. Rebuild and test

✨ **Current Status**: Migration complete!
