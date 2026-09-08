# CLAUDE.md — ministock-api

Este archivo se carga automáticamente en cada sesión de Claude Code dentro de esta carpeta.
Define la arquitectura, estándares y convenciones del proyecto. **Todo el código nuevo debe seguirlos.**

---

## Sobre el proyecto

**MiniStock** — sistema de gestión de inventario para pymes.
Stack: .NET 8 Web API + Next.js 14 (App Router) + PostgreSQL.

---

## Deploy

| Recurso | URL |
|---|---|
| Frontend | https://ministock.marcosrios.dev (Vercel) |
| API | https://api.ministock.marcosrios.dev |
| API directa | Hetzner (server `portfolio-hel1-1`) puerto `:5010`, Docker |

CI/CD: GitHub Actions (push → build → deploy). Estado: completo y deployado.

---

## Pendientes

- La migración `AddCategoryNameUniqueIndexAndUserRefreshTokenIndex` (rama `hardening/rbac-and-query-perf`) agrega un índice único a `Categories.Name`. Si la base de producción tuviera dos categorías con el mismo nombre, fallaría al aplicarse en el startup y el contenedor no levantaría. Como la DB de producción es solo datos de demo (nada real todavía), no hace falta reconciliar nada a mano si llegara a chocar: se puede resetear la base sin problema (`docker compose down -v` + `up` en el server, o borrar el volumen `pgdata`). **Ojo**: `DatabaseSeeder` solo corre `if (app.Environment.IsDevelopment())` (`Program.cs`) — en producción (`ASPNETCORE_ENVIRONMENT=Production`) un reset deja la base vacía, sin admin ni datos de demo, no se repuebla sola. Después de resetear en producción hay que re-crear el usuario admin a mano (o correr el seeder manualmente apuntando a esa base) antes de anunciar el demo como disponible de nuevo.
- Grabar video Loom de 90s con la demo (dashboard + CRUD de productos) y subirlo.
- Bump de Next.js 14→16 en `web/` — requiere `npm audit fix --force` (breaking cambios), dejado afuera a propósito de la auditoría de seguridad para no mezclarlo con fixes de seguridad. Hacerlo como migración aparte, con testing dedicado. Confirmado 2026-09-08 que sigue habiendo ~20 advisories abiertos en Next 14.2.35 + 2 altos en postcss (`npm audit`).
- Actualizar `CORS_ORIGINS` en el `.env` del server si todavía apunta a `http://localhost:3000` en vez de la URL real de Vercel/dominio propio.
- Tests de integración con `WebApplicationFactory<Program>` (ver sección "Tests" más abajo) — hoy solo hay tests unitarios (services con mocks, algunos con EF Core InMemory). No hay ningún test que levante la API completa end-to-end.
- CSP del frontend con `'unsafe-inline'` + `'unsafe-eval'` en `script-src` (`next.config.mjs`) — aceptado por ahora (ver `### Seguridad y performance` abajo). Arreglarlo bien requiere CSP con nonces, que a su vez requiere `middleware.ts` server-side — mismo trade-off ya documentado para el auth guard client-side.
- `DashboardService.GetSummaryAsync` sigue haciendo 4 round-trips separados a la DB (3 de ellos sobre `Products` con el mismo filtro `IsActive`) — se podrían combinar en 1-2 queries. Prioridad baja, no se tocó en el pase 2026-09-08 para no inflar el diff.
- Sin índice en `StockMovements.CreatedAt` (usado en el `ORDER BY` de todo listado de movimientos) — irrelevante a la escala actual, revisar si la tabla crece mucho.

### Seguridad y performance de queries (revisión 2026-09-08, resuelta en `hardening/rbac-and-query-perf`)

Seguridad:
- [x] **Broken Access Control** — el rol `Admin` existía en el dominio pero no se usaba en ningún lado; cualquier usuario autenticado (registro libre, sin aprobación) podía borrar/desactivar productos y categorías del demo público. Ahora `DELETE /products/{id}` y `DELETE /categories/{id}` requieren `[Authorize(Roles = RoleNames.Admin)]`.
- [x] **`Category.Name` sin índice único en DB** — la única protección contra duplicados era el chequeo `ExistsByNameAsync` en el servicio (race condition entre el chequeo y el `SaveChanges`). Se agregó `HasIndex(c => c.Name).IsUnique()` + migración. De paso, `ExistsByNameAsync` pasó a case-insensitive (`ILike` sin wildcards) para ser consistente con el chequeo de rename en `UpdateAsync`, que ya era case-insensitive.
- [x] **`Category.DeactivateAsync` no chequeaba productos activos server-side** — el frontend deshabilitaba el botón de baja si `productCount > 0`, pero la API igual la permitía si se llamaba directo. Ahora el servicio la rechaza con `409 Conflict`.
- [x] **`/auth/refresh` sin rate limiting** — `register` y `login` tenían `[EnableRateLimiting("auth")]`, `refresh` no. Emparejado.
- Next.js 14 (~20 CVEs) y CSP con `unsafe-inline`/`unsafe-eval` — confirmados, quedan en Pendientes arriba (fuera de alcance de este pase).

Performance (EF Core):
- [x] **`CategoryRepository` cargaba toda la colección de `Products` solo para contar** (`.Include(c => c.Products)` en `GetByIdAsync`/`GetAllActiveAsync`/`GetPagedAsync`, usado únicamente para `category.Products.Count`). Reemplazado por `GetActiveProductCountAsync`/`GetActiveProductCountsAsync`, que proyectan el conteo en SQL sin traer las filas de producto — mismo patrón que ya usaba `DashboardRepository.GetStockByCategoryAsync`. De paso corrige que el conteo mostrado incluía productos inactivos (contradecía el tooltip "Tiene productos activos" del frontend).
- [x] **Ningún query de solo lectura usaba `.AsNoTracking()`** — agregado en `ProductRepository`, `CategoryRepository`, `StockMovementRepository` y `UserRepository`. Seguro en este código porque los paths de escritura llaman explícitamente a `Update(entity)`, que adjunta y marca modificado sin depender del change tracker.
- [x] **`Users.RefreshToken` sin índice** — cada `/auth/refresh` escaneaba toda la tabla `Users`. Agregado con la misma migración del índice de `Categories.Name`.

### Mejoras de buenas prácticas y SOLID (revisión 2026-09-07, resuelta en `refactor/solid-cleanup`)

Backend (.NET):
- [x] **DIP** — Los 5 servicios de `Application/Services/` ahora implementan una interfaz (`IProductService`, `ICategoryService`, `IStockMovementService`, `IDashboardService`, `IAuthService`) y los controllers inyectan la interfaz.
- [x] **Regla de dependencia rota** — el id del rol por defecto se movió a `Role.WellKnownIds` en Domain; `AuthController` ya no referencia `MiniStock.Infrastructure` en absoluto.
- [x] **`Result` con tipo de error débil** — reemplazado por `Result.Type: ErrorType` (`Validation`/`NotFound`/`Conflict`/`Unauthorized`), mapeado a status code por `ResultExtensions.ToActionResult()` en la capa Api (ver "Result Pattern" más abajo).
- [x] **Status code inconsistente** — `ProductService.CreateAsync` con categoría inexistente ahora devuelve 404, no 409.
- [x] **DRY** — el mapeo `StockMovement → StockMovementResponse` está unificado en `StockMovementService.MapToResponse` (compartido con `DashboardService`).
- [x] **Regla de negocio** — `ProductRepository.ExistsBySkuAsync` ahora solo considera productos activos; el SKU de un producto dado de baja se puede reutilizar.
- [x] `AuthController.Register` ya no usa `CreatedAtAction` apuntando a sí mismo; devuelve `201` directo.
- [x] **Validación no conectada** (encontrado durante la implementación, no estaba en la revisión original) — los validators de FluentValidation estaban registrados en DI pero nunca se invocaban en ningún lado; la API aceptaba cualquier payload sin validar. Se agregó `ValidationFilter` (`IAsyncActionFilter` global) que corre el `IValidator<T>` de cada argumento antes del controller.
- [x] Excepciones no controladas → `GlobalExceptionHandler` (`IExceptionHandler` nativo de .NET 8) devuelve `ProblemDetails` 500 sin stack trace fuera de `Development`.
- [x] Mapster estaba registrado en DI (`IMapper`, `TypeAdapterConfig`) sin un solo uso real en el código — eliminado.

Frontend (Next.js):
- [x] **DRY entre páginas** — `productos/page.tsx`, `categorias/page.tsx` y `movimientos/page.tsx` ahora comparten `<Modal>`, `<ConfirmDialog>` y `<Pagination>` (`web/src/components/ui/`), el hook `useDebouncedValue` y el helper `getErrorMessage` (`web/src/lib/errors.ts`).

---

## Arquitectura — Clean Architecture

Fuente: [The Clean Architecture — Robert C. Martin (Uncle Bob)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
Referencia .NET: [Microsoft — Microservices architecture](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/)

### Regla de dependencia (estricta)

```
MiniStock.Domain
    ↑
MiniStock.Application
    ↑               ↑
MiniStock.Infrastructure   MiniStock.Api
```

Las capas internas **nunca** referencian capas externas.
Domain no conoce EF Core. Application no conoce ASP.NET. Infrastructure no conoce Http.

### Responsabilidades por capa

| Proyecto | Responsabilidad | Puede referenciar |
|---|---|---|
| `MiniStock.Domain` | Entidades, Value Objects, interfaces de dominio, excepciones de dominio | Nada externo |
| `MiniStock.Application` | Casos de uso, Commands/Queries, DTOs, interfaces de repositorios, validaciones | Solo Domain |
| `MiniStock.Infrastructure` | DbContext, repositorios, migraciones EF Core, servicios externos | Application + Domain |
| `MiniStock.Api` | Controllers, middleware, DI, configuración, Swagger | Application (nunca Domain directo) |

---

## Patrones de diseño

### Repository Pattern
Fuente: [Microsoft — Infrastructure persistence layer design](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

- Las interfaces de repositorio viven en `Application/Interfaces/`.
- Las implementaciones concretas viven en `Infrastructure/Repositories/`.
- Los controllers nunca tocan EF Core directamente.

```csharp
// Application/Interfaces/IProductRepository.cs
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Product>> GetPagedAsync(int page, int size, string? search, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Product product, CancellationToken ct = default);
}
```

### Result Pattern (en lugar de excepciones para lógica de negocio)
Fuente: [Andrew Lock — Working with the Result Pattern](https://andrewlock.net/series/working-with-the-result-pattern/)

- Los casos de uso retornan `Result<T>` o `Result`, nunca lanzan excepciones de negocio.
- Las excepciones se reservan para errores inesperados (infraestructura, bugs) — `GlobalExceptionHandler`
  (`MiniStock.Api/GlobalExceptionHandler.cs`, `IExceptionHandler` nativo de .NET 8) las convierte en un
  `ProblemDetails` 500 sin filtrar detalles fuera de `Development`.
- Cada `Result.Failure(...)` fallido lleva un `ErrorType` (`Validation` / `NotFound` / `Conflict` / `Unauthorized`).
  Los controllers **nunca** eligen el status code a mano ni comparan el string del error — usan
  `result.ToActionResult()` (`MiniStock.Api/Extensions/ResultExtensions.cs`), que centraliza el mapeo
  `ErrorType → status code + ProblemDetails` una sola vez.

```csharp
// Correcto
public async Task<Result<ProductDto>> CreateAsync(CreateProductCommand cmd, CancellationToken ct)
{
    if (await _repo.ExistsByNameAsync(cmd.Name, ct))
        return Result.Failure<ProductDto>("Ya existe un producto con ese nombre.", ErrorType.Conflict);
    // ...
    return Result.Success(dto);
}

// En el controller
var result = await _service.CreateAsync(cmd, ct);
return result.ToActionResult(value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));

// Incorrecto — no lanzar BusinessException para flujo normal
throw new BusinessException("Ya existe...");
```

### Validaciones con FluentValidation
Fuente: [FluentValidation — documentación oficial](https://fluentvalidation.net/)

- Un `AbstractValidator<TCommand>` por cada Command/Query en `Application/Validators/`.
- Registrar con `AddValidatorsFromAssembly` en el DI.
- Los controllers no validan manualmente: `ValidationFilter` (`MiniStock.Api/Filters/ValidationFilter.cs`,
  registrado globalmente en `Program.cs`) resuelve el `IValidator<T>` de cada argumento y corta con 400 antes
  de llegar al controller/servicio si falla. **Importante**: `AddValidatorsFromAssembly` solo registra los
  validators en DI — sin este filtro (u otro mecanismo que los invoque) no se ejecutan nunca.

---

## Convenciones de código

### Nombrado general
- Clases, métodos, propiedades: **PascalCase**.
- Variables locales, parámetros: **camelCase**.
- Constantes: **SCREAMING_SNAKE_CASE**.
- Archivos: mismo nombre que la clase que contienen.
- Idioma de código: **inglés** (nombres, comentarios de código).
- Idioma de respuesta en esta sesión: **español**.

### Commands y Queries (CQRS light)
No usamos MediatR para mantener la solución simple. Los casos de uso son clases de servicio en `Application/Services/`.

```
Application/
  Services/
    Products/
      ProductService.cs       ← métodos: GetPagedAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync
    Categories/
      CategoryService.cs
    Auth/
      AuthService.cs
  DTOs/
    Products/
      ProductDto.cs
      CreateProductRequest.cs
      UpdateProductRequest.cs
  Interfaces/
    IProductRepository.cs
    ICategoryRepository.cs
    IUnitOfWork.cs
  Validators/
    CreateProductValidator.cs
```

### Endpoints REST
Fuente: [Microsoft — Web API Design Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design)

- Recursos en plural y minúsculas: `/api/products`, `/api/categories`.
- Versioning en URL: `/api/v1/products`.
- HTTP status codes semánticos: 200, 201, 204, 400, 401, 403, 404, 409, 500.
- Paginación con query params: `?page=1&size=20&search=termo`.
- Respuesta de error estandarizada: [`ProblemDetails`](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors) (RFC 7807), el estándar nativo de ASP.NET Core — no un formato propio. Errores de negocio y excepciones no controladas devuelven `status`/`title`/`detail`; los fallos de validación de FluentValidation devuelven además `errors` (diccionario campo → mensajes), vía `ValidationProblemDetails`:

```json
// Error de negocio o excepción no controlada (ResultExtensions / GlobalExceptionHandler)
{
  "status": 404,
  "title": "Resource not found",
  "detail": "Categoría no encontrada."
}

// Fallo de validación (ValidationFilter)
{
  "status": 400,
  "title": "Validation error",
  "errors": {
    "Name": ["'Name' must not be empty."],
    "Price": ["'Price' must be greater than '0'."]
  }
}
```

### Logging
Fuente: [Serilog — sitio oficial](https://serilog.net/)

- Usar Serilog con sink a consola (y Azure Monitor en producción).
- Nunca loguear passwords, tokens ni datos sensibles.
- Nivel mínimo en desarrollo: `Debug`. En producción: `Information`.

---

## Git Flow

Fuente: [A successful Git branching model — Vincent Driessen](https://nvie.com/posts/a-successful-git-branching-model/)

### Ramas principales

| Rama | Propósito |
|---|---|
| `main` | Código en producción. Solo recibe merges desde `release/*` o `hotfix/*`. |
| `develop` | Integración continua. Base para todas las features. |

### Ramas de soporte

| Prefijo | Cuándo crear | Se mergea en |
|---|---|---|
| `feature/` | Nueva funcionalidad | `develop` |
| `fix/` | Bug en desarrollo | `develop` |
| `release/` | Preparar versión para producción | `main` + `develop` |
| `hotfix/` | Bug crítico en producción | `main` + `develop` |
| `chore/` | Setup, configs, sin lógica de negocio | `develop` |

### Flujo típico de una feature

```bash
git checkout develop
git pull origin develop
git checkout -b feature/product-crud
# ... trabajo ...
git push origin feature/product-crud
# Pull Request → develop
```

---

## Conventional Commits

Fuente: [Conventional Commits v1.0.0 — spec oficial](https://www.conventionalcommits.org/en/v1.0.0/)

### Formato

```
<tipo>(<scope>): <descripción corta en inglés>

[cuerpo opcional]

[footer opcional: BREAKING CHANGE, Closes #123]
```

### Tipos permitidos

| Tipo | Cuándo usarlo |
|---|---|
| `feat` | Nueva funcionalidad visible al usuario |
| `fix` | Corrección de bug |
| `chore` | Setup, dependencias, config, sin cambios funcionales |
| `refactor` | Cambio de código sin corregir bug ni agregar feature |
| `test` | Agregar o modificar tests |
| `docs` | Documentación únicamente |
| `ci` | Cambios en pipelines de CI/CD |
| `perf` | Mejora de performance |

### Scopes del proyecto

`auth` | `products` | `categories` | `stock` | `dashboard` | `db` | `api` | `web` | `infra`

### Ejemplos

```
feat(products): add pagination to product listing endpoint
fix(auth): correct refresh token expiration calculation
chore(db): add initial EF Core migration
refactor(products): extract product validation to FluentValidation
test(auth): add integration tests for login endpoint
docs(api): update Swagger descriptions for stock endpoints
```

### Reglas
- Descripción en **inglés**, en minúsculas, sin punto final.
- Máximo 72 caracteres en la primera línea.
- Usar cuerpo cuando el "por qué" no es obvio.
- `BREAKING CHANGE:` en el footer cuando se rompe compatibilidad.

---

## Variables de entorno y secrets

- **Nunca** hardcodear connection strings, JWT secrets, ni API keys.
- En desarrollo: `appsettings.Development.json` (gitignoreado) o `dotnet user-secrets`.
- En producción: variables de entorno del hosting (Render / Azure App Service).
- El archivo `.env` del frontend nunca se commitea (`.gitignore` lo excluye).

---

## Tests

- Tests de integración para todos los endpoints de auth y CRUD.
- Usar `WebApplicationFactory<Program>` con base de datos en memoria o PostgreSQL de test.
- Naming: `MetodoATestear_Escenario_ResultadoEsperado` (ej. `Login_ValidCredentials_ReturnsJwtToken`).
- No aspirar a 100% de coverage — priorizar los happy paths y los edge cases de negocio.

---

## Checklist antes de hacer PR

- [ ] Código compila sin warnings.
- [ ] Tests pasan (`dotnet test`).
- [ ] No hay secrets hardcodeados.
- [ ] Swagger refleja los nuevos endpoints.
- [ ] El nombre del branch sigue el prefijo correcto (`feature/`, `fix/`, etc.).
- [ ] El commit sigue Conventional Commits.
- [ ] El PR apunta a `develop`, no a `main`.
