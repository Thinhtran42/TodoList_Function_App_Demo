# TodoApp - ASP.NET Core Web API + Worker + PostgreSQL

Ứng dụng CRUD Todo với kiến trúc Clean Architecture, bao gồm:

- **Web API** (ASP.NET Core) - REST API với Swagger/OpenAPI
- **Worker Service** - Background jobs (Hangfire) + RabbitMQ consumer
- **PostgreSQL** - Database + Hangfire storage
- **RabbitMQ** - Message broker
- **MinIO** - Object storage

## 🏗️ Kiến trúc

```
TodoApp/
├── src/
│   ├── TodoApp.API/          # Web API (Controllers, Swagger)
│   ├── TodoApp.Worker/       # Background service (Hangfire + RabbitMQ)
│   ├── TodoApp.Application/  # Business logic, DTOs, Services
│   ├── TodoApp.Domain/       # Entities, Enums, Exceptions
│   └── TodoApp.Infrastructure/ # Data access, External services
├── tests/
│   └── TodoApp.Tests/        # Unit tests
├── docs/                     # Documentation
└── docker-compose.yml        # Docker deployment
```

## 🚀 Quick Start

### Option 1: Docker (Recommended)

**Bước 1: Clone repository**

```bash
git clone https://github.com/Thinhtran42/TodoList_Function_App_Demo.git
cd TodoList_Function_App_Demo
```

**Bước 2: Khởi động services**

```bash
# Build và start tất cả services
docker-compose up --build -d

# Chờ PostgreSQL khởi động (khoảng 10-15 giây)
```

**Bước 3: Khởi tạo database**

```bash
# Import database schema (bao gồm users, todos, authentication)
docker exec -i todoapp-postgres psql -U postgres -d tododb < init-db.sql
```

**Bước 4: Quản lý services**

```bash
# Xem logs tất cả services
docker-compose logs -f

# Xem logs của service cụ thể
docker-compose logs -f todoapp-api

# Restart với code mới
docker-compose down
docker-compose up --build -d

# Stop tất cả services
docker-compose down

# Stop và xóa volumes (reset database)
docker-compose down -v
```

**🌐 Services Available:**

- **API:** http://localhost:5231
- **Swagger UI:** http://localhost:5231/swagger
- **Hangfire Dashboard:** http://localhost:5231/hangfire
- **RabbitMQ Management:** http://localhost:15672 (username: `guest`, password: `guest`)
- **MinIO Console:** http://localhost:9001 (username: `minioadmin`, password: `minioadmin`)

**👤 Test Accounts:**

Sau khi import database, bạn có thể login với:

- Username: `testuser` hoặc `admin`
- Password: `password123`

**⚠️ Lưu ý:** Lần chạy đầu tiên **BẮT BUỘC** phải import database schema (Bước 3).

### Option 2: Local Development

**Prerequisites:**

- .NET SDK 8.0
- Docker Desktop (cho PostgreSQL, RabbitMQ, MinIO)
- Visual Studio 2022 hoặc VS Code

**Bước 1: Start Infrastructure**

```bash
# Chỉ chạy database, message broker, storage
docker-compose up -d postgres rabbitmq minio
```

**Bước 2: Setup Database**

```bash
# Import database schema
psql -h localhost -U postgres -d tododb -f init-db.sql
```

**Bước 3: Run API**

```bash
cd src/TodoApp.API
dotnet run
```

**Bước 4: Run Worker**

```bash
cd src/TodoApp.Worker
dotnet run
```

## 📚 Documentation

- [Docker Deployment Guide](docs/DOCKER_API.md) - Chi tiết về Docker deployment
- [Worker - RabbitMQ Consumer](docs/WORKER_RABBITMQ.md) - CSV import consumer
- [Worker - Hangfire Jobs](docs/WORKER_HANGFIRE.md) - Background monitoring jobs

## 🔧 Features

- ✅ CRUD Operations cho Todos
- ✅ User Authentication (JWT)
- ✅ CSV Import/Export
- ✅ Async processing với RabbitMQ
- ✅ Background jobs với Hangfire
- ✅ Object storage với MinIO
- ✅ Pagination, Filtering, Sorting
- ✅ Swagger/OpenAPI documentation
- ✅ Docker support
- ✅ Clean Architecture

## 📦 Tech Stack

- **Backend:** ASP.NET Core 8.0 Web API
- **Worker:** .NET Worker Service 8.0
- **Database:** PostgreSQL 16
- **Message Broker:** RabbitMQ
- **Object Storage:** MinIO
- **Background Jobs:** Hangfire
- **Authentication:** JWT
- **ORM:** Npgsql (raw SQL)
- **Documentation:** Swagger/OpenAPI
