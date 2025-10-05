# Service Bus Explorer Setup Guide

## 📥 Download Service Bus Explorer

### Option 1: GitHub Release (Recommended)
1. Go to: https://github.com/paolosalvatori/ServiceBusExplorer
2. Click "Releases" → Download latest version
3. Extract and run `ServiceBusExplorer.exe`

### Option 2: Microsoft Store
- Search "Service Bus Explorer" in Microsoft Store
- Install directly

## 🔗 Connection Setup

### 1. Get ServiceBus Connection String
```bash
# Create ServiceBus namespace (if not exists)
az servicebus namespace create \
  --name todoapp-servicebus-dev \
  --resource-group rg-todoapp-dev \
  --location eastus \
  --sku Basic

# Get connection string
az servicebus namespace authorization-rule keys list \
  --resource-group rg-todoapp-dev \
  --namespace-name todoapp-servicebus-dev \
  --name RootManageSharedAccessKey
```

### 2. Connect in Service Bus Explorer
1. Open Service Bus Explorer
2. Click "File" → "Connect"
3. Enter connection string:
   ```
   Endpoint=sb://todoapp-servicebus-dev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...
   ```

## 📋 Create Queue for Testing

### Via Service Bus Explorer:
1. Right-click namespace → "Create Queue"
2. Queue Name: `import-csv-queue`
3. Settings:
   - Max Size: 1 GB
   - Message TTL: 14 days
   - Lock Duration: 30 seconds
   - Enable Dead Lettering: Yes

### Via Azure CLI:
```bash
az servicebus queue create \
  --resource-group rg-todoapp-dev \
  --namespace-name todoapp-servicebus-dev \
  --name import-csv-queue \
  --max-size 1024 \
  --default-message-time-to-live P14D
```

## 🧪 Testing with Service Bus Explorer

### Send Test Message:
1. Right-click queue → "Send Messages"
2. Message Body:
   ```json
   {
     "jobId": "test-123",
     "userId": 1,
     "blobUrl": "https://storage.blob.core.windows.net/csv/test.csv"
   }
   ```
3. Click "Send"

### Receive Messages:
1. Right-click queue → "Receive Messages"  
2. Select "Peek and Lock" or "Receive and Delete"
3. View message content and properties

### Monitor Queue:
- View active messages count
- Check dead letter queue
- Monitor message throughput
- View queue properties

## 📊 Features Available:

### Message Operations:
- ✅ Send messages
- ✅ Receive/peek messages  
- ✅ Dead letter queue management
- ✅ Message resubmission
- ✅ Bulk message operations

### Queue Management:
- ✅ Create/delete queues
- ✅ View queue properties
- ✅ Monitor metrics
- ✅ Configure settings

### Advanced Features:
- ✅ Message filtering
- ✅ Correlation ID tracking
- ✅ Custom properties
- ✅ Import/export messages