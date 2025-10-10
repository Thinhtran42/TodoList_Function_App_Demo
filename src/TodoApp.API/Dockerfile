# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY TodoApp.sln ./

# Copy all project files
COPY src/TodoApp.Domain/TodoApp.Domain.csproj src/TodoApp.Domain/
COPY src/TodoApp.Application/TodoApp.Application.csproj src/TodoApp.Application/
COPY src/TodoApp.Infrastructure/TodoApp.Infrastructure.csproj src/TodoApp.Infrastructure/
COPY src/TodoApp.API/TodoApp.API.csproj src/TodoApp.API/

# Restore dependencies
RUN dotnet restore src/TodoApp.API/TodoApp.API.csproj

# Copy all source code
COPY src/TodoApp.Domain/ src/TodoApp.Domain/
COPY src/TodoApp.Application/ src/TodoApp.Application/
COPY src/TodoApp.Infrastructure/ src/TodoApp.Infrastructure/
COPY src/TodoApp.API/ src/TodoApp.API/

# Build the application
WORKDIR /src/src/TodoApp.API
RUN dotnet build TodoApp.API.csproj -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish TodoApp.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Environment variables (can be overridden)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "TodoApp.API.dll"]
