# TodoApp Web API

ASP.NET Core Web API implementation of TodoApp with JWT Authentication.

## 🚀 Features

- ✅ **Authentication & Authorization** - JWT-based authentication
- ✅ **Todo Management** - Full CRUD operations
- ✅ **CSV Import/Export** - Bulk operations
- ✅ **Filtering & Pagination** - Advanced query support
- ✅ **Swagger UI** - Interactive API documentation
- ✅ **Clean Architecture** - Separation of concerns
- ✅ **Multiple Database Support** - PostgreSQL & Cosmos DB

## 📋 Prerequisites

- .NET 8.0 SDK
- PostgreSQL or Azure Cosmos DB
- Azure Storage (for CSV import/export)
- Azure Service Bus (for async import)

## 🛠️ Getting Started

### 1. Configuration

Update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "TodoDb": "Host=localhost;Port=5432;Database=todoapp;Username=your_user;Password=your_password",
    "CosmosDb": "AccountEndpoint=https://...;AccountKey=...;",
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;",
    "ServiceBus": "Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=..."
  },
  "DatabaseProvider": "CosmosDB",
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong",
    "Issuer": "TodoApp",
    "Audience": "TodoApp",
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  }
}
```

### 2. Run the API

```bash
cd src/TodoApp.API
dotnet run
```

The API will start at: `http://localhost:5231`

### 3. Access Swagger UI

Open your browser: `http://localhost:5231/swagger`

## 📚 API Endpoints

### Authentication

| Method | Endpoint                    | Description          |
| ------ | --------------------------- | -------------------- |
| POST   | `/api/auth/register`        | Register new user    |
| POST   | `/api/auth/login`           | User login           |
| POST   | `/api/auth/refresh`         | Refresh access token |
| POST   | `/api/auth/logout`          | User logout          |
| POST   | `/api/auth/change-password` | Change password      |
| GET    | `/api/auth/profile`         | Get user profile     |
| PUT    | `/api/auth/profile`         | Update user profile  |

### Todos

| Method | Endpoint                     | Description              |
| ------ | ---------------------------- | ------------------------ |
| GET    | `/api/todos`                 | Get todos (with filters) |
| GET    | `/api/todos/{id}`            | Get todo by ID           |
| POST   | `/api/todos`                 | Create new todo          |
| PUT    | `/api/todos/{id}`            | Update todo              |
| DELETE | `/api/todos/{id}`            | Delete todo              |
| PATCH  | `/api/todos/{id}/complete`   | Mark as completed        |
| PATCH  | `/api/todos/{id}/uncomplete` | Mark as uncompleted      |

### Import/Export

| Method | Endpoint                     | Description           |
| ------ | ---------------------------- | --------------------- |
| POST   | `/api/todos/import`          | Import CSV (async)    |
| POST   | `/api/todos/import/sync`     | Import CSV (sync)     |
| GET    | `/api/todos/import/template` | Download CSV template |
| GET    | `/api/todos/export`          | Export to Azure Blob  |
| GET    | `/api/todos/export/download` | Direct CSV download   |

### Health Check

| Method | Endpoint      | Description      |
| ------ | ------------- | ---------------- |
| GET    | `/api/health` | Check API health |

## 🔐 Authentication Flow

1. **Register or Login**

   ```bash
   POST /api/auth/register
   {
     "email": "user@example.com",
     "password": "YourPassword123!",
     "fullName": "John Doe"
   }
   ```

2. **Get Access Token**

   ```json
   {
     "accessToken": "eyJhbGciOiJIUzI1NiIs...",
     "refreshToken": "...",
     "expiresIn": 3600
   }
   ```

3. **Use Token in Swagger**

   - Click "Authorize" button 🔒
   - Enter: `Bearer {your-access-token}`
   - Click "Authorize" → "Close"

4. **Call Protected Endpoints**
   - All requests now include the JWT token

## 📊 Query Parameters (GET /api/todos)

```
?isCompleted=false
&priority=High
&category=Work
&searchTerm=urgent
&tags=important,work
&sortBy=DueDate
&sortDescending=true
&page=1
&pageSize=10
```

## 📤 CSV Import Format

```csv
Title,Description,DueDate,Priority,IsCompleted,Category,Tags
Buy groceries,Get milk and bread,2025-12-31,Medium,false,Personal,shopping;home
Finish report,Complete quarterly report,2025-11-15,High,false,Work,work;urgent
```

## 🏗️ Project Structure

```
TodoApp.API/
├── Controllers/
│   ├── AuthController.cs      # Authentication endpoints
│   ├── TodosController.cs     # Todo CRUD operations
│   ├── ImportController.cs    # CSV import
│   ├── ExportController.cs    # CSV export
│   └── HealthController.cs    # Health check
├── Program.cs                 # App configuration
├── appsettings.json          # Base settings
└── appsettings.Development.json  # Dev settings
```

## 🔄 Differences from Azure Functions

| Feature         | Azure Functions       | Web API                 |
| --------------- | --------------------- | ----------------------- |
| **Hosting**     | Serverless            | Self-hosted/Container   |
| **Routing**     | `[HttpTrigger]`       | `[HttpGet/Post/etc]`    |
| **Auth**        | Custom JWT middleware | Built-in JWT middleware |
| **Swagger**     | OpenAPI extension     | Native Swashbuckle      |
| **File Upload** | `HttpRequestData`     | `IFormFile`             |
| **Response**    | `HttpResponseData`    | `IActionResult`         |

## 🐳 Docker Support (Optional)

```bash
# Build image
docker build -t todoapp-api .

# Run container
docker run -p 5231:8080 \
  -e ConnectionStrings__TodoDb="..." \
  -e JwtSettings__SecretKey="..." \
  todoapp-api
```

## 📝 Notes

- **Development**: Uses `appsettings.Development.json`
- **Production**: Uses environment variables or Azure App Settings
- **Secrets**: Never commit `appsettings.Development.json` to Git
- **Database**: Auto-creates Cosmos DB containers on first run

## 🆘 Troubleshooting

### Port Already in Use

```bash
# Change port in launchSettings.json or:
dotnet run --urls "http://localhost:5232"
```

### HTTPS Certificate Error

```bash
dotnet dev-certs https --trust
```

### Database Connection Error

- Check connection string
- Ensure database is running
- Verify firewall rules

## 📖 Related Projects

- **Functions**: `src/TodoApp.Functions/` - Azure Functions version
- **Domain**: `src/TodoApp.Domain/` - Business entities
- **Application**: `src/TodoApp.Application/` - Business logic
- **Infrastructure**: `src/TodoApp.Infrastructure/` - Data access

---

Made with ❤️ using ASP.NET Core 8.0
