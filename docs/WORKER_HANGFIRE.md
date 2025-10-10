# TodoApp Worker - Background Jobs với Hangfire

## Tổng quan

Worker project sử dụng Hangfire để xử lý các background jobs:

1. **CSV Import Worker** - Consumer RabbitMQ để xử lý CSV import
2. **Database Change Monitor** - Recurring job theo dõi thay đổi trong DB

## Hangfire Jobs

### 1. Database Change Monitor

**Job:** `DatabaseChangeMonitorJob`

**Recurring Jobs được cấu hình:**

#### Monitor All Changes (Chạy mỗi phút)

```csharp
Cron: Cron.Minutely (*/1 * * * *)
Job: MonitorAllChangesAsync()
```

Theo dõi tất cả thay đổi trong database (todos và users).

#### Monitor Todo Changes (Chạy mỗi 30 giây)

```csharp
Cron: */30 * * * * * (every 30 seconds)
Job: MonitorTodoChangesAsync()
```

Chỉ theo dõi thay đổi trong bảng todos.

### Các chức năng monitoring:

- **New Records**: Phát hiện records mới được tạo
- **Updated Records**: Phát hiện records được cập nhật
- **Logging**: Ghi log chi tiết về các thay đổi

## Cấu hình

### Hangfire Settings

Trong `Program.cs`:

```csharp
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2; // Số worker đồng thời
    options.ServerName = "TodoApp.Worker";
});
```

### Database

Hangfire sử dụng cùng PostgreSQL connection với ứng dụng chính:

- Schema: `hangfire`
- Tables: Tự động được tạo khi khởi động

## Chạy Worker

```bash
cd src/TodoApp.Worker
dotnet run
```

## Hangfire Dashboard

Để xem dashboard, có thể thêm vào API project:

```csharp
// In API Program.cs
app.UseHangfireDashboard("/hangfire");
```

Truy cập: `http://localhost:5231/hangfire`

## Thêm Recurring Jobs

Trong `Program.cs`, thêm job mới:

```csharp
recurringJobManager.AddOrUpdate<YourJob>(
    "job-id",
    job => job.YourMethod(),
    Cron.Daily, // Hoặc custom cron expression
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    });
```

## Cron Expressions

Một số examples:

```
Cron.Minutely       // Mỗi phút
Cron.Hourly         // Mỗi giờ
Cron.Daily          // Mỗi ngày
Cron.Weekly         // Mỗi tuần
Cron.Monthly        // Mỗi tháng
"*/30 * * * * *"    // Mỗi 30 giây
"0 0 * * *"         // Mỗi ngày lúc 00:00
"0 */4 * * *"       // Mỗi 4 giờ
```

## Logs

Worker ghi log chi tiết về:

- Các records mới được tạo
- Các records được cập nhật
- Thời gian chạy job
- Lỗi (nếu có)

**Log level:** Information, Debug

**Sample log output:**

```
info: TodoApp.Worker.Jobs.DatabaseChangeMonitorJob[0]
      Found 8 new todo(s) created since 10/10/2025 02:30:00
info: TodoApp.Worker.Jobs.DatabaseChangeMonitorJob[0]
      New Todo: Id=123, Title=Learn Azure Functions, CreatedBy=3, CreatedAt=10/10/2025 02:31:15
```

## Troubleshooting

### Job không chạy

1. Check Hangfire logs trong terminal
2. Verify connection string trong `appsettings.Development.json`
3. Check schema `hangfire` tồn tại trong PostgreSQL

### Performance

Nếu có nhiều records:

- Tăng `WorkerCount` trong Hangfire config
- Optimize query trong job (thêm index, filter by timestamp)
- Giảm frequency của recurring job

## Mở rộng

### Thêm monitoring cho bảng khác

Tạo method mới trong `DatabaseChangeMonitorJob.cs`:

```csharp
public async Task MonitorYourTableAsync()
{
    // Your monitoring logic
}
```

Đăng ký trong `Program.cs`:

```csharp
recurringJobManager.AddOrUpdate<DatabaseChangeMonitorJob>(
    "monitor-your-table",
    job => job.MonitorYourTableAsync(),
    Cron.Minutely);
```

### Thêm notification

Tích hợp với notification service để gửi alert khi có thay đổi quan trọng:

```csharp
private readonly INotificationService _notificationService;

public async Task MonitorCriticalChangesAsync()
{
    // Detect changes
    if (criticalChangesDetected)
    {
        await _notificationService.SendAlertAsync("Critical changes detected!");
    }
}
```

## Dependencies

- **Hangfire.Core** (1.8.21)
- **Hangfire.AspNetCore** (1.8.21)
- **Hangfire.PostgreSql** (1.20.12)
- **RabbitMQ.Client** (6.8.1)

## Architecture

```
Worker Project
├── Jobs/
│   └── DatabaseChangeMonitorJob.cs    # Recurring jobs
├── Services/
│   └── CsvImportWorker.cs             # RabbitMQ consumer
├── Program.cs                          # Hangfire + DI config
└── appsettings.Development.json        # Configuration
```
