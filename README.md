# MiniStock — Sistema de Gestión de Inventario

**Demo en vivo:** https://web-ochre-zeta-22.vercel.app &nbsp;·&nbsp; **API Swagger:** http://204.168.134.159:5010/swagger

> Sistema fullstack de inventario con autenticación JWT, CRUD completo de productos/categorías/movimientos y dashboard con métricas en tiempo real.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com)
[![Next.js](https://img.shields.io/badge/Next.js-14-000000)](https://nextjs.org)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED)](https://www.docker.com)

---

## Screenshots

| Dashboard | Productos | Movimientos |
|:---------:|:---------:|:-----------:|
| ![Dashboard](docs/screenshots/dashboard.png) | ![Productos](docs/screenshots/productos.png) | ![Movimientos](docs/screenshots/movimientos.png) |

---

## Credenciales demo

| Campo    | Valor               |
|----------|---------------------|
| Email    | admin@ministock.com |
| Password | Admin123!           |

---

## Stack técnico

| Capa           | Tecnología                                                                 |
|----------------|----------------------------------------------------------------------------|
| Backend        | .NET 8 · ASP.NET Core · Clean Architecture (4 capas)                      |
| ORM / DB       | EF Core 8 · PostgreSQL 16 · migraciones automáticas al iniciar             |
| Auth           | JWT access tokens + refresh tokens · BCrypt · roles Admin/User            |
| Validación     | FluentValidation                                                           |
| Mapping        | Mapster                                                                    |
| Logging        | Serilog → Console                                                          |
| Frontend       | Next.js 14 (App Router) · TypeScript strict · Tailwind CSS                |
| Data fetching  | Tanstack Query v5 (React Query)                                            |
| HTTP client    | Axios con interceptor JWT + redirect 401 automático                        |
| Gráficos       | Recharts                                                                   |
| Deploy API     | Docker multi-stage · docker-compose · Hetzner VPS :5010                   |
| Deploy web     | Vercel (HTTPS automático)                                                  |
| CI/CD          | GitHub Actions → SSH → `docker compose up --build` en cada push a `main`  |

---

## Arquitectura

```
02-inventory-api/
├── api/
│   ├── MiniStock.Domain/          # Entidades, enums — sin dependencias externas
│   ├── MiniStock.Application/     # Servicios, DTOs, interfaces, validadores
│   ├── MiniStock.Infrastructure/  # EF Core, repositorios, JWT, seeder
│   └── MiniStock.Api/             # Controllers, Program.cs, Swagger
├── web/
│   └── src/
│       ├── app/
│       │   ├── (app)/             # Rutas protegidas (sidebar + auth guard)
│       │   ├── api/v1/[...path]/  # Proxy Route Handler → evita mixed-content
│       │   └── login/
│       ├── hooks/                 # useProducts, useCategories, useMovements…
│       └── lib/                   # api.ts (Axios + JWT), auth.ts (localStorage)
└── deploy/
    ├── Dockerfile                 # Multi-stage build .NET 8
    ├── docker-compose.yml         # API + PostgreSQL con health checks
    └── nginx.conf
```

### Decisiones de diseño

- **Proxy Route Handler:** el frontend en Vercel (HTTPS) no puede llamar directamente a la API en HTTP. Un Route Handler de Next.js actúa como proxy server-side, evitando el bloqueo mixed-content del browser. Solución: excluir el header `Expect` al reenviar la petición (Kestrel no lo soporta).
- **Result\<T\> pattern:** los servicios de Application devuelven `Result<T>` en lugar de lanzar excepciones de negocio. Los controllers mapean el resultado a HTTP sin try/catch.
- **Seed idempotente:** `DatabaseSeeder.SeedAsync` corre en cada startup pero solo inserta si `Categories` está vacía.
- **Soft delete:** productos y categorías tienen `IsActive` — nunca se borran físicamente.

---

## Setup local

### Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com)

### Backend

```bash
# 1. PostgreSQL local
docker run -d --name ministock-db \
  -e POSTGRES_DB=ministock \
  -e POSTGRES_USER=ministock \
  -e POSTGRES_PASSWORD=localpass \
  -p 5432:5432 postgres:16-alpine

# 2. Secrets (nunca en el código)
cd api/MiniStock.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=ministock;Username=ministock;Password=localpass"
dotnet user-secrets set "Jwt:Key" "clave-secreta-de-al-menos-32-caracteres"
dotnet user-secrets set "Jwt:Issuer" "ministock-api"
dotnet user-secrets set "Jwt:Audience" "ministock-web"

# 3. Correr — migra, seedea y arranca
dotnet run --project api/MiniStock.Api
# API en http://localhost:5197 · Swagger en http://localhost:5197/swagger
```

### Frontend

```bash
cd web
echo 'NEXT_PUBLIC_API_URL=http://localhost:5197/api/v1' > .env.local
npm install
npm run dev
# App en http://localhost:3000
```

---

## Deploy en producción

El deploy es automático con cada push a `main`:

```
git push origin main
# → GitHub Actions: dotnet build → SSH → docker compose up --build
```

### Secrets de GitHub Actions

| Secret            | Descripción         |
|-------------------|---------------------|
| `HETZNER_HOST`    | IP del servidor     |
| `HETZNER_USER`    | Usuario SSH         |
| `HETZNER_SSH_KEY` | Clave privada SSH   |

### Variables en el servidor (`deploy/.env`)

```env
DB_PASSWORD=...
JWT_KEY=...
CORS_ORIGINS=https://web-ochre-zeta-22.vercel.app
```

### Variables en Vercel

| Variable              | Valor                              |
|-----------------------|------------------------------------|
| `NEXT_PUBLIC_API_URL` | `/api/v1`                          |
| `API_BASE_URL`        | `http://<IP>:5010/api/v1`          |

---

## Endpoints

Todos requieren `Authorization: Bearer <token>` excepto `/auth/*`.

### Auth `POST /api/v1/auth`

| Método | Ruta        | Descripción                         |
|--------|-------------|-------------------------------------|
| POST   | `/register` | Registro (rol User por defecto)     |
| POST   | `/login`    | Login → accessToken + refreshToken  |
| POST   | `/refresh`  | Rotar tokens                        |
| POST   | `/logout`   | Revocar refresh token               |

### Products `/api/v1/products`

| Método | Ruta    | Descripción                                        |
|--------|---------|----------------------------------------------------|
| GET    | `/`     | Lista paginada `?page&pageSize&search&categoryId`  |
| GET    | `/{id}` | Producto por ID                                    |
| POST   | `/`     | Crear producto                                     |
| PUT    | `/{id}` | Editar producto                                    |
| DELETE | `/{id}` | Soft delete                                        |

### Categories `/api/v1/categories`

| Método | Ruta    | Descripción                       |
|--------|---------|-----------------------------------|
| GET    | `/`     | Lista paginada `?page&pageSize&search` |
| GET    | `/all`  | Todas activas (para dropdowns)    |
| GET    | `/{id}` | Categoría por ID                  |
| POST   | `/`     | Crear categoría                   |
| PUT    | `/{id}` | Editar categoría                  |
| DELETE | `/{id}` | Soft delete                       |

### Stock Movements `/api/v1/stock-movements`

| Método | Ruta | Descripción                               |
|--------|------|-------------------------------------------|
| POST   | `/`  | Registrar movimiento (Entry/Exit/Adjustment) |
| GET    | `/`  | Historial paginado `?page&pageSize&productId` |

### Dashboard `/api/v1/dashboard`

| Método | Ruta                  | Descripción                              |
|--------|-----------------------|------------------------------------------|
| GET    | `/summary`            | KPIs (total productos, valor, bajo stock) |
| GET    | `/stock-by-category`  | Stock agrupado por categoría (gráfico)   |
| GET    | `/low-stock`          | Productos con stock ≤ mínimo             |
| GET    | `/recent-movements`   | Últimos N movimientos                    |

---

## Estado del proyecto

- [x] Clean Architecture + EF Core + PostgreSQL
- [x] JWT auth — register / login / refresh / logout
- [x] CRUD Productos, Categorías, Movimientos de stock
- [x] Dashboard con KPIs y gráfico por categoría
- [x] Frontend Next.js 14 completo y conectado
- [x] Docker multi-stage + docker-compose
- [x] CI/CD con GitHub Actions
- [x] Deploy en producción (Hetzner + Vercel)
- [x] Seed de datos demo

---

## Autor

**Marcos Ríos** — Desarrollador Fullstack .NET / Next.js  
[Portfolio](https://portfolio-web-drab-ten.vercel.app/) · [LinkedIn](https://www.linkedin.com/in/marcos-sebasti%C3%A1n-r%C3%ADos-359b717/) · [GitHub](https://github.com/tech-marcos-rios)
