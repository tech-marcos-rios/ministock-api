# Proyecto 1 — Sistema de gestión: API REST + Dashboard

> Tu primer proyecto de portafolio. Es el más importante porque demuestra fullstack completo, autenticación y deploy. Tiempo estimado: **2-3 semanas**.

## ¿Qué construir?

Un mini sistema de gestión de inventario / clientes / pedidos (elegí uno). El dominio importa poco — lo que se evalúa es la calidad técnica.

**Sugerencia**: "MiniStock" — gestión de inventario para pymes con productos, categorías, movimientos de stock y alertas de stock bajo.

## Stack

**Backend**
- .NET 8 + ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL (gratis en Supabase o Neon)
- JWT con refresh tokens
- AutoMapper, FluentValidation
- Swagger / OpenAPI

**Frontend**
- Next.js 14 (App Router)
- TypeScript
- Tailwind CSS
- Recharts (gráficos)
- React Hook Form + Zod (formularios y validación)
- Tanstack Query (fetching y cache)

**Deploy**
- Backend: Azure App Service (free tier) o Render
- Frontend: Vercel (gratis)
- DB: Supabase (gratis)

## Features mínimos (MVP)

1. **Auth**: registro, login, logout. JWT con refresh token.
2. **Roles**: admin / usuario.
3. **CRUD productos**: crear, listar (con paginación + búsqueda), editar, eliminar.
4. **CRUD categorías**: relacionadas con productos.
5. **Movimientos de stock**: registrar entradas/salidas.
6. **Dashboard**: gráficos de stock por categoría, productos con stock bajo, movimientos recientes.
7. **Responsive**: tiene que verse bien en mobile.
8. **Deploy en producción** con dominio público.

## Arquitectura sugerida

```
proyecto-1-api-dashboard/
├── api/                            # Backend .NET
│   ├── MiniStock.Api/              # Web API project
│   ├── MiniStock.Application/      # Casos de uso, DTOs
│   ├── MiniStock.Domain/           # Entidades, lógica de dominio
│   ├── MiniStock.Infrastructure/   # EF Core, repositorios
│   └── MiniStock.sln
├── web/                            # Frontend Next.js
│   └── (estructura igual a portfolio-web)
└── README.md
```

## Plan paso a paso

### Día 1-2: Backend setup
- Crear solución .NET con los 4 proyectos (Clean Architecture).
- Configurar EF Core + PostgreSQL.
- Migración inicial con tablas: Users, Roles, Products, Categories, StockMovements.
- Endpoints de health check + Swagger.

### Día 3-5: Auth + CRUD
- Endpoints `/auth/register`, `/auth/login`, `/auth/refresh`.
- Middleware JWT, atributos `[Authorize]`.
- CRUD de productos y categorías.

### Día 6-7: Dashboard data
- Endpoints agregados: stock por categoría, top productos, movimientos recientes.
- Tests de integración básicos.

### Día 8-10: Frontend setup + auth
- Pantallas de login y registro.
- Layout con sidebar + topbar.
- Tanstack Query con interceptor para refresh tokens.

### Día 11-13: Pantallas CRUD
- Listado de productos con tabla, paginación, búsqueda.
- Form de crear/editar producto.
- Listado de categorías.
- Pantalla de movimientos de stock.

### Día 14-15: Dashboard + polish
- Página `/dashboard` con KPIs y gráficos.
- Modo oscuro (heredarlo de tu portfolio).
- Estados de carga y errores.

### Día 16-18: Deploy y documentación
- Dockerizar la API (opcional pero suma).
- Deploy backend en Azure App Service o Render.
- Deploy frontend en Vercel.
- README con capturas, cómo correrlo en local, link a la demo.
- Crear video de 90 segundos mostrando el sistema (Loom es gratis).

## Cómo trabajarlo con Claude Code

```bash
cd proyecto-1-api-dashboard
# Crea la solución .NET
dotnet new sln -n MiniStock
# A partir de acá pedile a Claude Code que arme cada proyecto.
```

Pídele a Claude Code (en una sesión separada dentro de la carpeta del proyecto):

> "Voy a construir una Web API en .NET 8 con Clean Architecture (4 proyectos: Api, Application, Domain, Infrastructure). Usaremos EF Core con PostgreSQL. Vamos a hacerlo paso a paso. Empezá creando los 4 proyectos y la solución, y mostrame los comandos dotnet."

Luego ve agregando features uno por uno, leyendo el código que genera y haciendo preguntas.

## Cómo presentarlo en el portafolio

- README con badges (build, deploy, license).
- Capturas de cada pantalla principal.
- Diagrama de arquitectura simple (Excalidraw o draw.io).
- Sección "Decisiones técnicas" explicando por qué Clean Architecture, por qué PostgreSQL, etc.
- Link a la demo en vivo.
- Credenciales de demo: `demo@demo.com / Demo123!`.

## Por qué este proyecto

Cubre prácticamente todos los requisitos típicos de un proyecto freelance: auth, CRUD, dashboard, deploy. Cuando un cliente vea tu portfolio, va a pensar "este flaco puede hacer mi sistema interno". Vale más que 10 landing pages.
