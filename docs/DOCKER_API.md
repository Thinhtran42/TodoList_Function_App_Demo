# TodoApp - Docker Deployment

## 📋 Prerequisites

- Docker 20.10+
- Docker Compose 2.0+

## 🚀 Quick Start

### Using Docker Compose (Recommended)

Start everything (API, Worker, Hangfire, PostgreSQL, RabbitMQ, MinIO):

```bash
docker-compose up -d
```

### Build and Run Manually

1. **Build the images:**

```bash
# Build API
docker build -f src/TodoApp.API/Dockerfile -t todoapp-api:latest .

# Build Worker
docker build -f src/TodoApp.Worker/Dockerfile -t todoapp-worker:latest .

# Or use the build script
chmod +x build-all.sh
./build-all.sh
```

2. **Run the containers:**

```bash
# Run API
docker run -d \
  -p 5231:8080 \
  --name todoapp-api \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__TodoDb="Host=host.docker.internal;Port=5432;Database=tododb;Username=postgres;Password=postgres" \
  todoapp-api:latest

# Run Worker
docker run -d \
  --name todoapp-worker \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__TodoDb="Host=host.docker.internal;Port=5432;Database=tododb;Username=postgres;Password=postgres" \
  todoapp-worker:latest
```

## 🌐 Access URLs

After starting with docker-compose:

**Application Services:**

- **API:** http://localhost:5231
- **Swagger UI:** http://localhost:5231/swagger
- **Hangfire Dashboard:** http://localhost:5231/hangfire (view worker jobs)
- **Health Check:** http://localhost:5231/health

**Infrastructure Services:**

- **RabbitMQ Management:** http://localhost:15672 (guest/guest)
- **MinIO Console:** http://localhost:9001 (minioadmin/minioadmin)
- **PostgreSQL:** localhost:5432 (postgres/postgres)

**Services Running:**

- ✅ **todoapp-api:** REST API with JWT authentication
- ✅ **todoapp-worker:** Background jobs (Hangfire) + CSV import consumer (RabbitMQ)
- ✅ **postgres:** Database with Hangfire schema
- ✅ **rabbitmq:** Message broker for async job processing
- ✅ **minio:** Object storage for CSV files

## 🔧 Environment Variables

Key environment variables you can override:

```bash
# Database
ConnectionStrings__TodoDb=Host=postgres;Port=5432;Database=tododb;Username=postgres;Password=postgres

# JWT
JwtSettings__SecretKey=your-secret-key-here
JwtSettings__Issuer=TodoApp
JwtSettings__Audience=TodoApp.API
JwtSettings__ExpirationMinutes=60

# MinIO
MinIO__Endpoint=minio:9000
MinIO__AccessKey=minioadmin
MinIO__SecretKey=minioadmin
MinIO__BucketName=todo-imports

# RabbitMQ
RabbitMQSettings__HostName=rabbitmq
RabbitMQSettings__Port=5672
RabbitMQSettings__QueueName=import-csv-queue
```

## 🛠️ Common Commands

### View logs:

```bash
# All services
docker-compose logs -f

# Specific service
docker logs -f todoapp-api
docker logs -f todoapp-worker

# Worker only (to see Hangfire + CSV import logs)
docker logs -f todoapp-worker --tail=100
```

### Stop services:

```bash
# Stop all services
docker-compose down
```

### Stop and remove volumes:

```bash
# Remove all data
docker-compose down -v
```

### Restart a service:

```bash
# Restart API
docker-compose restart todoapp-api

# Restart Worker
docker-compose restart todoapp-worker
```

### Scale Worker instances (for high load):

```bash
# Run 3 Worker instances with WorkerCount=2 each = 6 total Hangfire workers
docker-compose up -d --scale todoapp-worker=3
```

### Execute commands in container:

```bash
# Access API container
docker exec -it todoapp-api bash

# Access Worker container
docker exec -it todoapp-worker bash

# Check Worker logs in real-time
docker logs -f todoapp-worker | grep "Database change monitor"
```

## 🐛 Troubleshooting

### 1. Port conflicts

If ports are already in use, modify them in `docker-compose.api.yml`:

```yaml
services:
  todoapp-api:
    ports:
      - "8080:8080" # Change 5231 to another port
```

### 2. Database connection issues

Check if PostgreSQL is healthy:

```bash
docker-compose -f docker-compose.api.yml ps
docker logs todoapp-postgres
```

### 3. API not starting

Check API logs:

```bash
docker logs todoapp-api
```

### 4. Rebuild after code changes

```bash
docker-compose -f docker-compose.api.yml build --no-cache todoapp-api
docker-compose -f docker-compose.api.yml up -d todoapp-api
```

## 📊 Health Checks

The API includes built-in health checks:

- **Liveness:** `GET /health`
- **Docker Health:** Automatically checked every 30s

Check health status:

```bash
docker inspect --format='{{.State.Health.Status}}' todoapp-api
```

## 🔐 Production Considerations

Before deploying to production:

1. **Change default passwords:**

   - PostgreSQL: `POSTGRES_PASSWORD`
   - RabbitMQ: `RABBITMQ_DEFAULT_PASS`
   - MinIO: `MINIO_ROOT_PASSWORD`
   - JWT: `JwtSettings__SecretKey`

2. **Use environment files:**

```bash
docker-compose -f docker-compose.api.yml --env-file .env.production up -d
```

3. **Enable SSL/TLS:**

   - Set `MinIO__UseSSL=true`
   - Configure HTTPS for API
   - Use proper certificates

4. **Resource limits:**

```yaml
services:
  todoapp-api:
    deploy:
      resources:
        limits:
          cpus: "1"
          memory: 512M
        reservations:
          cpus: "0.5"
          memory: 256M
```

## 📦 Multi-stage Build Benefits

The Dockerfile uses multi-stage builds for:

- ✅ Smaller final image size (~200MB vs 2GB)
- ✅ Faster deployment
- ✅ Better security (only runtime dependencies)
- ✅ Separation of build and runtime environments

## 🔗 Related Services

To run the Worker service as well, see:

- `docker-compose.worker.yml`
- `src/TodoApp.Worker/Dockerfile`
