# Configuration Setup Guide

## Local Development Setup

### 1. Copy Sample Files
```bash
# Copy and rename sample files
cp appsettings.sample.json appsettings.json
cp local.settings.sample.json local.settings.json
```

### 2. Update Configuration Values

#### `appsettings.json`:
- Replace `YOUR_DB_USER` and `YOUR_DB_PASSWORD` with your PostgreSQL credentials
- Replace `YOUR_COSMOS_ACCOUNT` and `YOUR_COSMOS_KEY` with your Cosmos DB details
- Replace `YOUR_STORAGE_ACCOUNT` and `YOUR_STORAGE_KEY` with your Azure Storage details
- Replace `YOUR_SERVICEBUS_NAMESPACE` and `YOUR_SERVICEBUS_KEY` with your Service Bus details
- Replace `YOUR_JWT_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG` with a secure JWT secret

#### `local.settings.json`:
- Same as above, but also update:
- `AzureWebJobsStorage`: Azure Storage connection string for Functions runtime
- `ServiceBus`: Service Bus connection string for Functions triggers

### 3. Required Azure Resources
- **Cosmos DB Account**: For data storage
- **Azure Storage Account**: For blob storage and Functions runtime
- **Service Bus Namespace**: For message queuing
- **PostgreSQL Database**: (Optional, if using PostgreSQL instead of Cosmos DB)

### 4. Security Notes
- ⚠️ **NEVER commit `appsettings.json` or `local.settings.json` with real secrets**
- ✅ Only commit the `.sample.json` files with placeholder values
- 🔒 Use Azure Key Vault in production environments

### 5. Environment Variables (Production)
For production deployment, set these as App Settings in Azure Functions:
- `ConnectionStrings:CosmosDb`
- `ConnectionStrings:AzureStorage` 
- `ConnectionStrings:ServiceBus`
- `JwtSettings:SecretKey`

## Database Providers
This project supports multiple database providers:
- **CosmosDB** (default): Set `DatabaseProvider` to `"CosmosDB"`
- **PostgreSQL**: Set `DatabaseProvider` to `"PostgreSQL"`