# Local Development Setup

## 🏠 Local Emulators Available

### ✅ Azurite (Blob Storage Emulator)
```bash
# Install Azurite
npm install -g azurite

# Start Azurite
azurite --silent --location c:\azurite --debug c:\azurite\debug.log

# Or via Docker
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

**Connection String:**
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;
```

### ✅ Cosmos DB Emulator  
```bash
# Already have this running on localhost:8081
```

### ❌ ServiceBus Emulator
- **Not available** - Must use cloud ServiceBus
- **Alternative**: RabbitMQ for local development

## 🔄 Hybrid Approach

### Local Development Stack:
- **Cosmos DB**: Local emulator ✅
- **Blob Storage**: Azurite ✅  
- **ServiceBus**: Cloud (free tier) ⚠️
- **Functions**: Local development ✅

### Configuration:
```json
// local.settings.json
{
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1...",
    "CosmosConnectionString": "AccountEndpoint=https://localhost:8081/...",
    "ServiceBusConnectionString": "Endpoint=sb://todoapp-dev.servicebus.windows.net/..."
  }
}
```