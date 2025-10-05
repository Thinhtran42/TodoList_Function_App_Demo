# Todo Function API (.NET 8 isolated) + PostgreSQL

CRUD To-Do đơn giản viết bằng **Azure Functions (.NET 8 isolated)**, dùng **PostgreSQL** chạy bằng **Docker**. Có sẵn **OpenAPI/Swagger UI** để test.

## 1) Yêu cầu
- Docker Desktop
- .NET SDK 8.0
- Visual Studio 2022 (workload **Azure development**)
- (Tuỳ chọn) `psql` hoặc pgAdmin nếu muốn chạy SQL thủ công

## 2) Tạo file env:
PGUSER=todo
PGPASSWORD=todo123
PGDATABASE=todoapp

## 3) Chạy Postgres bằng Docker
- docker compose up -d

## 4) Chạy script trong file schema.sql với pgadmin

## 5) Chạy ứng dụng

- Mở solution bằng Visual Studio 2022

- Restore NuGet nếu cần

- F5 để chạy 
