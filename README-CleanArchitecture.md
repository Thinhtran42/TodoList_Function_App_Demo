# TodoApp - Clean Architecture with Azure Functions

This project demonstrates a **Clean Architecture** implementation using Azure Functions with Isolated Worker pattern, following Domain-Driven Design principles.

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    TodoApp.API                              │
│                (Presentation Layer)                         │
│           ┌─────────────────────────────────┐              │
│           │      Azure Functions           │              │
│           │   - TodoFunctions.cs           │              │
│           │   - HTTP Triggers               │              │
│           │   - OpenAPI Documentation      │              │
│           └─────────────────────────────────┘              │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                TodoApp.Application                          │
│                (Application Layer)                          │
│           ┌─────────────────────────────────┐              │
│           │        Use Cases               │              │
│           │   - CreateTodoUseCase          │              │
│           │   - GetTodoUseCase             │              │
│           │   - UpdateTodoUseCase          │              │
│           │   - DeleteTodoUseCase          │              │
│           └─────────────────────────────────┘              │
│           ┌─────────────────────────────────┐              │
│           │      Interfaces               │              │
│           │   - ITodoRepository           │              │
│           └─────────────────────────────────┘              │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                 TodoApp.Domain                              │
│                 (Domain Layer)                              │
│           ┌─────────────────────────────────┐              │
│           │       Entities                 │              │
│           │   - TodoItem                   │              │
│           │   - Business Rules             │              │
│           │   - Domain Logic               │              │
│           └─────────────────────────────────┘              │
│           ┌─────────────────────────────────┐              │
│           │      Exceptions                │              │
│           │   - TodoNotFoundException      │              │
│           │   - TodoValidationException    │              │
│           └─────────────────────────────────┘              │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│              TodoApp.Infrastructure                         │
│              (Infrastructure Layer)                         │
│           ┌─────────────────────────────────┐              │
│           │      Data Access               │              │
│           │   - TodoDbContext              │              │
│           │   - TodoRepository             │              │
│           │   - Entity Framework           │              │
│           └─────────────────────────────────┘              │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

```
TodoApp.sln
├── src/
│   ├── TodoApp.API/                    # 🎯 Presentation Layer
│   │   ├── Functions/
│   │   │   └── TodoFunctions.cs        # Azure Functions HTTP Triggers
│   │   ├── Program.cs                  # Application Entry Point
│   │   ├── appsettings.json           # Configuration
│   │   └── host.json                  # Function Host Configuration
│   │
│   ├── TodoApp.Application/            # 🔄 Application Layer
│   │   ├── DTOs/
│   │   │   └── TodoDtos.cs            # Data Transfer Objects
│   │   ├── Interfaces/
│   │   │   └── ITodoRepository.cs     # Repository Contracts
│   │   └── UseCases/
│   │       ├── CreateTodoUseCase.cs
│   │       ├── GetAllTodosUseCase.cs
│   │       ├── GetTodoByIdUseCase.cs
│   │       ├── UpdateTodoUseCase.cs
│   │       └── DeleteTodoUseCase.cs
│   │
│   ├── TodoApp.Domain/                 # 🏛️ Domain Layer
│   │   ├── Entities/
│   │   │   └── TodoItem.cs            # Domain Entity with Business Rules
│   │   └── Exceptions/
│   │       └── DomainExceptions.cs    # Domain-specific Exceptions
│   │
│   └── TodoApp.Infrastructure/         # 🔧 Infrastructure Layer
│       ├── Data/
│       │   └── TodoDbContext.cs       # Entity Framework DbContext
│       ├── Repositories/
│       │   └── TodoRepository.cs      # Repository Implementation
│       └── DependencyInjection.cs    # IoC Container Configuration
│
└── legacy/                            # 📦 Original Project (for reference)
    └── ToDoFunction.csproj
```

## 🎯 Clean Architecture Principles Applied

### 1. **Dependency Inversion**
- **Inner layers** (Domain, Application) don't depend on outer layers
- **Outer layers** (Infrastructure, API) depend on inner layers through interfaces
- Dependencies flow **inward** toward the Domain

### 2. **Separation of Concerns**
- **Domain**: Business entities and rules
- **Application**: Use cases and orchestration logic
- **Infrastructure**: Data access and external services
- **API**: HTTP presentation and request/response handling

### 3. **Single Responsibility**
- Each layer has a **single, well-defined responsibility**
- Each use case handles **one specific business operation**
- Each repository manages **one aggregate root**

### 4. **Interface Segregation**
- Small, focused interfaces (e.g., `ITodoRepository`)
- No client depends on methods it doesn't use

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL database
- Azure Functions Core Tools v4

### Configuration

Update `src/TodoApp.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "TodoDb": "Host=localhost;Port=5432;Database=todoapp;Username=todo;Password=todo123"
  }
}
```

### Build and Run

```bash
# Build the solution
dotnet build TodoApp.sln

# Run the API project
cd src/TodoApp.API
func start
```

## 📋 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET    | `/api/todos` | Get all todos |
| GET    | `/api/todos/{id}` | Get todo by ID |
| POST   | `/api/todos` | Create new todo |
| PUT    | `/api/todos/{id}` | Update existing todo |
| DELETE | `/api/todos/{id}` | Delete todo |

## 🏗️ Domain Model

### TodoItem Entity
```csharp
public class TodoItem
{
    public long Id { get; set; }
    public string Title { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Domain Business Rules
    public void MarkAsCompleted()
    public void MarkAsIncomplete()  
    public void UpdateTitle(string newTitle)
    public static TodoItem Create(string title)
}
```

### Business Rules
- **Title Validation**: Cannot be empty or exceed 500 characters
- **State Management**: Proper completed/incomplete state transitions
- **Audit Trail**: Automatic timestamp updates
- **Domain Events**: Rich domain model with behavior

## 🔄 Use Case Examples

### Create Todo Use Case
```csharp
public async Task<TodoDto> ExecuteAsync(CreateTodoRequest request)
{
    // 1. Domain validation through entity factory
    var todoItem = TodoItem.Create(request.Title);
    
    // 2. Persist through repository
    var createdTodo = await _todoRepository.CreateAsync(todoItem);
    
    // 3. Map to DTO for presentation layer
    return MapToDto(createdTodo);
}
```

## 🔧 Dependency Injection

### Infrastructure Registration
```csharp
// In TodoApp.Infrastructure/DependencyInjection.cs
services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(connectionString));

services.AddScoped<ITodoRepository, TodoRepository>();
```

### Application Registration  
```csharp
// In TodoApp.API/Program.cs
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<CreateTodoUseCase>();
builder.Services.AddScoped<GetTodoByIdUseCase>();
// ... other use cases
```

## ✅ Benefits of This Architecture

1. **Testability**: Each layer can be tested independently
2. **Maintainability**: Clear separation makes changes easier
3. **Flexibility**: Easy to swap implementations (e.g., database providers)
4. **Scalability**: Well-organized code scales better
5. **Domain Focus**: Business logic is protected and centralized
6. **Technology Independence**: Domain doesn't know about Azure Functions or EF

## 🔄 Migration from Legacy

The original monolithic structure has been refactored:

**Before (Monolithic):**
```
ToDoFunction/
├── Functions/           # Mixed concerns
├── Infrastructure/      # Tightly coupled
├── Contracts/          # Shared DTOs
└── Program.cs          # Everything together
```

**After (Clean Architecture):**
```
src/
├── TodoApp.Domain/      # Pure business logic
├── TodoApp.Application/ # Use cases and interfaces  
├── TodoApp.Infrastructure/ # Data access implementation
└── TodoApp.API/         # HTTP presentation layer
```

## 📚 Key Concepts

- **Entities**: `TodoItem` with rich behavior and business rules
- **Use Cases**: Single-purpose application services
- **Repository Pattern**: Abstract data access behind interfaces
- **DTO Mapping**: Clean separation between domain and presentation
- **Exception Handling**: Domain-specific exceptions with proper HTTP mapping
- **Logging**: Structured logging throughout all layers

This architecture provides a solid foundation for building maintainable, testable, and scalable Azure Functions applications!