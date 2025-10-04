# Phase 1: Import CSV Implementation Plan

## 🎯 Architecture Overview
```
HTTP POST /todos/import
├── 1. Upload CSV file to Blob Storage
├── 2. Send message to ServiceBus Queue
├── 3. Queue Trigger processes import
├── 4. Validate and parse CSV data
├── 5. Bulk insert to Cosmos DB
└── 6. Return import status/job ID
```

## 📁 Files to Create/Modify

### 1. Application Layer
- `TodoApp.Application/DTOs/ImportDtos.cs`
- `TodoApp.Application/Interfaces/ICsvImportService.cs`
- `TodoApp.Application/Services/CsvImportService.cs`

### 2. Infrastructure Layer  
- `TodoApp.Infrastructure/Services/CsvImportService.cs`
- Add ServiceBus configuration

### 3. API Layer
- `TodoApp.API/Functions/ImportFunctions.cs`
- `TodoApp.API/Functions/ImportQueueFunctions.cs`

## 🔧 Implementation Steps

### Step 1: Create DTOs and Interfaces
```csharp
// ImportDtos.cs
public class ImportRequest
{
    public IFormFile CsvFile { get; set; }
}

public class ImportResponse  
{
    public string JobId { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
}

public class ImportStatus
{
    public string JobId { get; set; }
    public string Status { get; set; } // Pending, Processing, Completed, Failed
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; }
}
```

### Step 2: HTTP Trigger (Upload)
```csharp
[Function("ImportTodos")]
public async Task<HttpResponseData> ImportTodos(
    [HttpTrigger("post", Route = "todos/import")] HttpRequestData req)
{
    // 1. Validate user & file
    // 2. Upload to blob storage
    // 3. Send message to ServiceBus
    // 4. Return job ID
}
```

### Step 3: ServiceBus Queue Trigger (Process)
```csharp
[Function("ProcessImport")]
public async Task ProcessImport(
    [ServiceBusTrigger("import-csv-queue")] string message)
{
    // 1. Download CSV from blob
    // 2. Parse and validate data
    // 3. Bulk insert to Cosmos DB
    // 4. Update import status
}
```

### Step 4: Status Check Endpoint
```csharp
[Function("GetImportStatus")]
public async Task<HttpResponseData> GetImportStatus(
    [HttpTrigger("get", Route = "todos/import/{jobId}")] HttpRequestData req)
{
    // Return current import status
}
```

## 🗃️ Database Schema
- Create `ImportJobs` container in Cosmos DB
- Track import status, progress, errors

## ⚙️ Configuration Required
- Azure ServiceBus namespace
- Import queue configuration
- Blob storage container for uploads

## 🧪 Testing Strategy
1. Unit tests for CSV parsing
2. Integration tests for ServiceBus flow  
3. End-to-end test: Upload → Process → Verify data

## 📊 Success Criteria
- [ ] Can upload CSV via HTTP
- [ ] File stored in blob storage
- [ ] ServiceBus message sent
- [ ] Queue trigger processes file
- [ ] Data imported to Cosmos DB
- [ ] Status tracking works
- [ ] Error handling comprehensive