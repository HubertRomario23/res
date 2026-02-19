# ResultViewer

A hybrid ingestion/retrieval POC for viewing test run results. Built with **Clean Architecture** using .NET 8 (backend) and Vue 3 (frontend).

## Architecture Overview

```
┌──────────┐     ┌────────────┐     ┌──────────────┐     ┌─────────────┐
│  Vue SPA │────▶│  ASP.NET   │────▶│  SQL Server  │     │ NightBatch  │
│  (Vite)  │     │  Core API  │     │  (EF Core)   │     │ File System │
└──────────┘     └────────────┘     └──────────────┘     └─────────────┘
                       │                                        │
                       └────── Fallback if not in DB ───────────┘
```

### Read Flow (DB-first, filesystem fallback)

1. Client requests a test run by `host/pdc/runId`
2. API checks in-memory cache
3. Cache miss → query SQL Server
4. DB miss → scan NightBatch filesystem for raw XML
5. Parse XML → upsert into DB → archive to Server Machine
6. Return DTO to client

## Project Structure

```
resultviewer/
├── backend/
│   ├── ResultViewer.Domain/          # Entities, repository interfaces
│   ├── ResultViewer.Application/     # DTOs, mappers, service interfaces & impls
│   ├── ResultViewer.Infrastructure/  # EF Core DbContext, repositories, file/XML services
│   ├── ResultViewer.Api/             # ASP.NET Core Web API (controllers, middleware)
│   ├── ResultViewer.Tests/           # xUnit unit tests
│   ├── ResultViewer.sln
│   └── Dockerfile
├── frontend/
│   ├── src/
│   │   ├── api/          # Axios API client
│   │   ├── stores/       # Pinia state management
│   │   ├── router/       # Vue Router
│   │   ├── views/        # RunsList, RunDetail pages
│   │   └── main.js
│   ├── Dockerfile
│   └── nginx.conf
├── docker-compose.yml
└── README.md
```

## Tech Stack

| Layer          | Technology                          |
|----------------|-------------------------------------|
| Frontend       | Vue 3, Vite, Vue Router, Pinia, Axios |
| Backend API    | ASP.NET Core 8 Web API              |
| ORM            | Entity Framework Core 8 (SQL Server)|
| Logging        | Serilog (Console + File sinks)      |
| Caching        | IMemoryCache                        |
| Testing        | xUnit, Moq                          |
| Containerization | Docker, docker-compose            |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (optional, for containerized deployment)
- SQL Server instance (or use the Docker Compose setup)

## Getting Started

### 1. Backend (local)

```bash
cd backend

# Restore & build
dotnet build ResultViewer.sln

# Run tests
dotnet test ResultViewer.Tests

# Apply EF Core migrations (requires SQL Server connection)
dotnet ef database update --project ResultViewer.Infrastructure --startup-project ResultViewer.Api

# Run the API
dotnet run --project ResultViewer.Api
# API available at https://localhost:5001 and http://localhost:5000
```

### 2. Frontend (local)

```bash
cd frontend
npm install
npm run dev
# App available at http://localhost:5173
```

### 3. Docker Compose (all-in-one)

```bash
docker-compose up --build
# Frontend: http://localhost:8080
# API:      http://localhost:5000
# SQL:      localhost:1433
```

## API Endpoints

| Method | URL                        | Description                                  |
|--------|----------------------------|----------------------------------------------|
| GET    | `/api/runs?host=&pdc=&runId=` | Get a single run (DB-first, file fallback) |
| GET    | `/api/runs/list?page=&pageSize=&host=&pdc=&result=&fromDate=&toDate=` | Paginated list with filters |
| GET    | `/api/health`              | Simple liveness check                        |
| GET    | `/health`                  | ASP.NET Core health check (incl. SQL)        |

## Configuration

Key settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost,1433;Database=ResultViewer;..."
  },
  "NightBatch": {
    "RootPath": "C:\\NightBatch\\Results"
  },
  "Archive": {
    "RootPath": "C:\\Archive\\ResultViewer"
  },
  "Retention": {
    "Years": 2,
    "ArchiveYears": 5
  }
}
```

## EF Core Migrations

```bash
cd backend

# Create a new migration
dotnet ef migrations add InitialCreate --project ResultViewer.Infrastructure --startup-project ResultViewer.Api

# Apply migrations
dotnet ef database update --project ResultViewer.Infrastructure --startup-project ResultViewer.Api
```

## License

Internal use only.
