# MiniStock API

Inventory management REST API built with .NET 8 and Clean Architecture. Part of my fullstack portfolio — the backend powers a Next.js dashboard (in progress).

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com)
[![EF Core](https://img.shields.io/badge/EF_Core-8.0-512BD4)](https://learn.microsoft.com/en-us/ef/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## Features

- **Auth** — JWT access tokens + refresh tokens, BCrypt password hashing, role-based (Admin / User)
- **Products** — full CRUD, pagination, case-insensitive search by name or SKU, soft delete
- **Categories** — full CRUD, pagination, flat list endpoint for dropdowns
- **Stock movements** — Entry / Exit / Adjustment types, automatic stock update, insufficient-stock guard
- **Swagger UI** — available in development at `/swagger`
- **Health check** — `GET /health`

## Tech stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| Web framework | ASP.NET Core Web API |
| ORM | Entity Framework Core 8 + Npgsql |
| Database | PostgreSQL 16 |
| Auth | JWT Bearer + refresh tokens |
| Validation | FluentValidation |
| Mapping | Mapster |
| Logging | Serilog → Console |
| Docs | Swashbuckle / Swagger |

## Architecture

Clean Architecture with 4 layers, strict dependency rule (outer → inner only):

```
MiniStock.Domain          — entities, enums, no external dependencies
MiniStock.Application     — services, DTOs, repository interfaces, validators
MiniStock.Infrastructure  — EF Core, repositories, JWT service, migrations
MiniStock.Api             — controllers, DI wiring, Swagger, middleware
```

## API endpoints

All endpoints require `Authorization: Bearer <token>` except auth.

### Auth — `POST /api/v1/auth`
| Method | Path | Description |
|---|---|---|
| POST | `/register` | Create account (assigned User role) |
| POST | `/login` | Get access + refresh tokens |
| POST | `/refresh` | Rotate tokens |
| POST | `/logout` | Revoke refresh token |

### Products — `/api/v1/products`
| Method | Path | Description |
|---|---|---|
| GET | `/` | Paged list — `?page&pageSize&search&categoryId` |
| GET | `/{id}` | Single product |
| POST | `/` | Create product |
| PUT | `/{id}` | Update product |
| DELETE | `/{id}` | Soft deactivate |

### Categories — `/api/v1/categories`
| Method | Path | Description |
|---|---|---|
| GET | `/` | Paged list — `?page&pageSize&search` |
| GET | `/all` | All active categories (for dropdowns) |
| GET | `/{id}` | Single category |
| POST | `/` | Create category |
| PUT | `/{id}` | Update category |
| DELETE | `/{id}` | Soft deactivate |

### Stock movements — `/api/v1/stock-movements`
| Method | Path | Description |
|---|---|---|
| POST | `/` | Register movement (Entry / Exit / Adjustment) |
| GET | `/` | Paged list — `?page&pageSize&productId` |
| GET | `/recent` | Latest N movements — `?count=10` (for dashboard widget) |

## Running locally

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com) (for PostgreSQL)

### 1. Start the database

```bash
docker run -d --name ministock-postgres \
  -e POSTGRES_USER=ministock \
  -e POSTGRES_PASSWORD=ministock123 \
  -e POSTGRES_DB=ministock \
  -p 5433:5432 \
  postgres:16-alpine
```

### 2. Configure secrets

Edit `api/MiniStock.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=ministock;Username=ministock;Password=ministock123"
  },
  "Jwt": {
    "Key": "your-secret-key-min-32-characters-long",
    "Issuer": "ministock-api",
    "Audience": "ministock-web"
  }
}
```

### 3. Apply migrations and run

```bash
# Apply migrations
dotnet ef database update \
  --project api/MiniStock.Infrastructure \
  --startup-project api/MiniStock.Api

# Run
dotnet run --project api/MiniStock.Api --launch-profile http
```

API available at `http://localhost:5197` — Swagger at `http://localhost:5197/swagger`.

### Demo credentials

After running, register at `POST /api/v1/auth/register` with any email/password, or use:

```
email: demo@ministock.com
password: Demo123!
```

## Project status

- [x] Clean Architecture setup
- [x] JWT auth (register / login / refresh / logout)
- [x] EF Core migrations + role seed (Admin / User)
- [x] Products CRUD
- [x] Categories CRUD
- [x] Stock movements (Entry / Exit / Adjustment)
- [ ] Dashboard aggregate endpoints
- [ ] Next.js frontend
- [ ] Docker Compose (API + DB)
- [ ] CI/CD with GitHub Actions
- [ ] Production deploy (Hetzner)

## License

MIT
