# TodoApp Worker - RabbitMQ Consumer

Background service để consume messages từ RabbitMQ queue và xử lý import CSV files.

## Chức năng

Worker này lắng nghe messages từ RabbitMQ queue `import-csv-queue` và xử lý import CSV files tự động khi có message được gửi từ API's ImportController.

## Cấu trúc

```
TodoApp.Worker/
├── Program.cs                          # Entry point, cấu hình DI
├── Services/
│   └── RabbitMQConsumerService.cs     # Background service consume RabbitMQ
├── appsettings.json                   # Production configuration
└── appsettings.Development.json       # Development configuration
```

## Prerequisites

1. **PostgreSQL**: Database để lưu todos
2. **MinIO**: Object storage để lưu CSV files
3. **RabbitMQ**: Message broker để giao tiếp giữa API và Worker

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "TodoDb": "Host=localhost;Port=5432;Database=todoapp;Username=postgres;Password=postgres"
  },
  "DatabaseProvider": "PostgreSQL",
  "StorageProvider": "MinIO",
  "MessageBrokerProvider": "RabbitMQ",
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "todo-files",
    "UseSSL": false
  },
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

## Cách chạy

### 1. Đảm bảo các services đang chạy

```bash
# RabbitMQ
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# PostgreSQL
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:15

# MinIO
docker run -d --name minio -p 9000:9000 -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  minio/minio server /data --console-address ":9001"
```

Hoặc sử dụng docker-compose ở root project:

```bash
docker-compose up -d
```

### 2. Build và chạy Worker

```bash
# Build project
dotnet build

# Chạy Worker
dotnet run

# Hoặc chạy với watch mode (tự động reload khi có thay đổi)
dotnet watch run
```

## Workflow

1. **User upload CSV** qua API endpoint `POST /api/todos/import`
2. **API** upload file lên MinIO và gửi message vào RabbitMQ queue
3. **Worker** nhận message từ queue
4. **Worker** download CSV từ MinIO
5. **Worker** parse và import todos vào database
6. **Worker** acknowledge message (thành công) hoặc nack (thất bại)

## Message Format

Worker nhận message với format:

```json
{
  "UserId": "123",
  "FileUrl": "todo-files/import_20231010_123456.csv",
  "FileName": "todos.csv",
  "RequestId": "550e8400-e29b-41d4-a716-446655440000",
  "RequestedAt": "2023-10-10T12:34:56Z"
}
```

## Monitoring

### Logs

Worker sử dụng Microsoft.Extensions.Logging với các log levels:

- **Information**: Thông tin xử lý bình thường
- **Warning**: Import có lỗi nhưng vẫn tiếp tục
- **Error**: Lỗi nghiêm trọng, message sẽ được requeue

### RabbitMQ Management UI

Truy cập http://localhost:15672

- Username: `guest`
- Password: `guest`

Tại đây bạn có thể:

- Xem số lượng messages trong queue
- Monitor consumer status
- Manually requeue/purge messages

## Error Handling

- **Transient errors**: Message được requeue và retry
- **Permanent errors**: Message được acknowledged để tránh infinite loop
- **Parse errors**: Log chi tiết và skip record đó

## Performance

- **Prefetch Count**: 1 (xử lý 1 message tại một thời điểm)
- **Manual Acknowledgment**: Chỉ acknowledge khi xử lý thành công
- **Auto Recovery**: RabbitMQ connection tự động reconnect khi mất kết nối

## Testing

### Manual test workflow:

1. Start Worker:

```bash
cd src/TodoApp.Worker
dotnet run
```

2. Upload CSV qua API (trong terminal khác):

```bash
curl -X POST http://localhost:7071/api/todos/import \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@test.csv"
```

3. Xem logs của Worker để theo dõi quá trình xử lý

## Troubleshooting

### Worker không nhận được messages

1. Check RabbitMQ đang chạy:

```bash
docker ps | grep rabbitmq
```

2. Check queue exists và có messages:

- Truy cập http://localhost:15672
- Vào tab "Queues"
- Tìm queue `import-csv-queue`

3. Check connection string trong appsettings.json

### Database connection errors

1. Verify PostgreSQL đang chạy
2. Check connection string
3. Verify database `todoapp` đã được tạo

### MinIO connection errors

1. Verify MinIO đang chạy
2. Check MinIO endpoint và credentials
3. Verify bucket `todo-files` đã được tạo

## Development

### Thêm processing logic mới

Edit file `Services/RabbitMQConsumerService.cs`, method `ProcessMessageAsync`

### Thay đổi queue configuration

Edit `RabbitMQSettings` trong appsettings.json và restart Worker

## Production Deployment

### Systemd Service (Linux)

Tạo file `/etc/systemd/system/todoapp-worker.service`:

```ini
[Unit]
Description=TodoApp Background Worker
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/todoapp/worker
ExecStart=/usr/bin/dotnet /opt/todoapp/worker/TodoApp.Worker.dll
Restart=always
RestartSec=10
User=todoapp
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Enable và start service:

```bash
sudo systemctl enable todoapp-worker
sudo systemctl start todoapp-worker
sudo systemctl status todoapp-worker
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/TodoApp.Worker/TodoApp.Worker.csproj", "src/TodoApp.Worker/"]
COPY ["src/TodoApp.Infrastructure/TodoApp.Infrastructure.csproj", "src/TodoApp.Infrastructure/"]
COPY ["src/TodoApp.Application/TodoApp.Application.csproj", "src/TodoApp.Application/"]
COPY ["src/TodoApp.Domain/TodoApp.Domain.csproj", "src/TodoApp.Domain/"]
RUN dotnet restore "src/TodoApp.Worker/TodoApp.Worker.csproj"
COPY . .
WORKDIR "/src/src/TodoApp.Worker"
RUN dotnet build "TodoApp.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TodoApp.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoApp.Worker.dll"]
```

Build và run:

```bash
docker build -t todoapp-worker -f src/TodoApp.Worker/Dockerfile .
docker run -d --name worker todoapp-worker
```
