# CLAUDE.md — inventory-api (MiniStock)

Este archivo se carga automáticamente en cada sesión de Claude Code dentro de esta carpeta.
Define la arquitectura, estándares y convenciones del proyecto. **Todo el código nuevo debe seguirlos.**

---

## Sobre el proyecto

**MiniStock** — sistema de gestión de inventario para pymes.
Stack: .NET 8 Web API + Next.js 14 (App Router) + PostgreSQL (Supabase).
Deploy: backend en Render, frontend en Vercel.

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
- Las excepciones se reservan para errores inesperados (infraestructura, bugs).
- Un `GlobalExceptionMiddleware` convierte excepciones no controladas en respuestas HTTP 500.

```csharp
// Correcto
public async Task<Result<ProductDto>> Handle(CreateProductCommand cmd, CancellationToken ct)
{
    if (await _repo.ExistsByNameAsync(cmd.Name, ct))
        return Result.Failure<ProductDto>("Ya existe un producto con ese nombre.");
    // ...
    return Result.Success(dto);
}

// Incorrecto — no lanzar BusinessException para flujo normal
throw new BusinessException("Ya existe...");
```

### Validaciones con FluentValidation
Fuente: [FluentValidation — documentación oficial](https://fluentvalidation.net/)

- Un `AbstractValidator<TCommand>` por cada Command/Query en `Application/Validators/`.
- Registrar con `AddValidatorsFromAssembly` en el DI.
- Los controllers no validan manualmente: el middleware de validación lo hace antes de llegar al handler.

### Mapeo con AutoMapper
- Profiles en `Application/Mappings/`.
- Un profile por entidad de dominio.
- Nunca mapear en el controller ni en el repositorio.

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
- Respuesta de error estandarizada:

```json
{
  "status": 400,
  "title": "Validation error",
  "errors": ["El nombre es requerido", "El precio debe ser mayor a 0"]
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
