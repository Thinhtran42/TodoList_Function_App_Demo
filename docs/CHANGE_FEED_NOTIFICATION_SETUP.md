# Todo Notification System với Change Feed và Service Bus

## 📋 Tổng quan

Hệ thống này implement một flow event-driven sử dụng:

- **Cosmos DB Change Feed** - Lắng nghe mọi thay đổi trong database
- **Azure Service Bus Topic** - Pub/Sub messaging cho notifications
- **Azure Functions** - Xử lý events và gửi notifications

## 🏗️ Kiến trúc

```
Cosmos DB (TodoItems Container)
    ↓ (Change Feed)
CosmosChangeFeedTrigger Function
    ↓ (Send message)
Service Bus Topic (todo-notify-topic)
    ↓ (Multiple subscriptions)
ServiceBusTopicTrigger Functions
    ↓ (Log to console)
Console Output
```

## 🚀 Setup

### 1. Cài đặt dependencies

```bash
cd src/TodoApp.API
dotnet restore
```

### 2. Tạo Service Bus Topic và Subscriptions

**macOS/Linux:**

```bash
chmod +x setup-servicebus-topic.sh
./setup-servicebus-topic.sh
```

**Windows:**

```powershell
.\setup-servicebus-topic.ps1
```

Hoặc tạo thủ công qua Azure Portal:

1. Vào Service Bus Namespace của bạn
2. Tạo Topic mới: `todo-notify-topic`
3. Tạo 2 Subscriptions:
   - `todo-notification-subscription`
   - `todo-notification-subscription-2`

### 3. Cấu hình Connection Strings

Kiểm tra `local.settings.json` đã có đầy đủ:

```json
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://...;AccountKey=...;",
    "ServiceBus": "Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...;"
  }
}
```

### 4. Tạo Lease Container trong Cosmos DB

Change Feed cần một container để lưu lease state. Có 2 cách:

**Cách 1: Tự động** (Function sẽ tạo)

- Container `leases` sẽ được tạo tự động khi function chạy lần đầu

**Cách 2: Thủ công** (khuyến nghị)

```bash
# Sử dụng Azure Portal hoặc CLI
az cosmosdb sql container create \
  --account-name your-cosmos-account \
  --database-name TodoApp \
  --name leases \
  --partition-key-path "/id" \
  --throughput 400
```

## 🎯 Các Functions

### 1. TodoChangeFeedTrigger

- **Trigger:** Cosmos DB Change Feed
- **Chức năng:**
  - Lắng nghe mọi thay đổi trong `TodoItems` container
  - Phân loại event type (Created/Updated)
  - Gửi notification message lên Service Bus Topic

### 2. TodoNotificationHandler (Subscription 1)

- **Trigger:** Service Bus Topic Subscription
- **Chức năng:**
  - Nhận notification từ topic
  - Log chi tiết với format đẹp
  - Complete message

### 3. TodoNotificationHandler_Subscription2 (Subscription 2)

- **Trigger:** Service Bus Topic Subscription
- **Chức năng:**
  - Nhận cùng notification (Pub/Sub pattern)
  - Log format đơn giản hơn
  - Demonstrate multiple consumers

## 🧪 Testing

### Bước 1: Chạy Function App

```bash
cd src/TodoApp.API
func start
# hoặc sử dụng VS Code task
```

### Bước 2: Tạo một Todo mới

```bash
# Login và lấy token
curl -X POST http://localhost:7071/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "your-user", "password": "your-password"}'

# Tạo todo mới
curl -X POST http://localhost:7071/api/todos \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Change Feed",
    "description": "Testing notification system",
    "priority": 2,
    "category": 1
  }'
```

### Bước 3: Update Todo

```bash
curl -X PUT http://localhost:7071/api/todos/{id} \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Updated Todo",
    "isCompleted": true
  }'
```

### Bước 4: Kiểm tra Console Logs

Bạn sẽ thấy output như sau:

```
=================================================
📢 TODO NOTIFICATION RECEIVED
=================================================
Message ID: 123e4567-e89b-12d3-a456-426614174000
Subject: Created
Content Type: application/json
Enqueued Time: 2025-10-06T10:30:45.123Z
─────────────────────────────────────────────────
📋 EVENT TYPE: Created
🆔 TODO ID: 638641234567890123
📝 TITLE: Test Change Feed
👤 USER ID: 1
⏰ TIMESTAMP: 2025-10-06 10:30:45
🔥 PRIORITY: Medium
📂 CATEGORY: Work
✅ COMPLETED: No
─────────────────────────────────────────────────
🎉 Todo 'Test Change Feed' has been created at 2025-10-06 10:30:45
=================================================
```

## 🔍 Troubleshooting

### Change Feed không trigger

1. Kiểm tra Cosmos DB connection string
2. Verify container name: `TodoItems`
3. Kiểm tra lease container đã được tạo
4. Xem logs để tìm errors

### Service Bus không nhận message

1. Verify topic và subscriptions đã được tạo
2. Kiểm tra connection string
3. Verify topic name trong code = topic name trong Azure
4. Check IAM permissions nếu dùng Managed Identity

### Message bị Dead Letter

1. Check deserialization errors
2. Verify message format
3. Check subscription max delivery count
4. Review lock duration settings

## 📊 Monitoring

### Application Insights

Nếu đã enable Application Insights, bạn có thể:

- Track custom events
- Query logs với KQL
- Setup alerts cho failures

### Azure Portal

- Monitor Change Feed processing lag
- Check Service Bus message counts
- View dead letter queue

## 🎨 Customization

### Thêm filters cho Change Feed

```csharp
[CosmosDBTrigger(
    StartFromBeginning = true,  // Process existing documents
    MaxItemsPerInvocation = 100,  // Batch size
    FeedPollDelay = 1000  // Poll every 1 second
)]
```

### Thêm message filters cho Subscriptions

Trong Azure Portal, tạo SQL Filter Rules:

```sql
-- Chỉ nhận Created events
EventType = 'Created'

-- Chỉ nhận High priority todos
Priority >= 3
```

### Custom event types

Update `DetermineEventType()` method để handle soft deletes:

```csharp
private string DetermineEventType(CosmosTodoItem document)
{
    if (document.isDeleted)  // Nếu có soft delete field
        return "Deleted";

    var timeDiff = (document.updatedAt - document.createdAt).TotalSeconds;
    return timeDiff < 2 ? "Created" : "Updated";
}
```

## 📚 Resources

- [Cosmos DB Change Feed](https://learn.microsoft.com/azure/cosmos-db/change-feed)
- [Service Bus Topics](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-queues-topics-subscriptions)
- [Azure Functions Triggers](https://learn.microsoft.com/azure/azure-functions/functions-triggers-bindings)
