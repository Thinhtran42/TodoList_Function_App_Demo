# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY TodoApp.sln ./

# Copy all project files
COPY src/TodoApp.Domain/TodoApp.Domain.csproj src/TodoApp.Domain/
COPY src/TodoApp.Application/TodoApp.Application.csproj src/TodoApp.Application/
COPY src/TodoApp.Infrastructure/TodoApp.Infrastructure.csproj src/TodoApp.Infrastructure/
COPY src/TodoApp.Worker/TodoApp.Worker.csproj src/TodoApp.Worker/

# Restore dependencies
RUN dotnet restore src/TodoApp.Worker/TodoApp.Worker.csproj

# Copy all source code
COPY src/TodoApp.Domain/ src/TodoApp.Domain/
COPY src/TodoApp.Application/ src/TodoApp.Application/
COPY src/TodoApp.Infrastructure/ src/TodoApp.Infrastructure/
COPY src/TodoApp.Worker/ src/TodoApp.Worker/

# Build the application
WORKDIR /src/src/TodoApp.Worker
RUN dotnet build TodoApp.Worker.csproj -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish TodoApp.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Environment variables (can be overridden)
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "TodoApp.Worker.dll"]
