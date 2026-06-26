# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Comandos principales

Todos los comandos se ejecutan desde `Backend/`:

```bash
# Restaurar dependencias
dotnet restore

# Compilar
dotnet build

# Correr la API (desde JazFinanzasApp.API/)
cd JazFinanzasApp.API && dotnet run

# Tests
dotnet test

# Un test específico
dotnet test --filter "FullyQualifiedName~TransactionServiceTests"

# Migraciones EF Core (desde JazFinanzasApp.API/)
dotnet ef migrations add <NombreMigracion>
dotnet ef database update
```

La API queda disponible en `https://localhost:7203` (Swagger UI en `/swagger`).

## Arquitectura — capas

```
JazFinanzasApp.API/
├── Controllers/           # Recibe/valida requests HTTP, delega al servicio
├── Business/
│   ├── DTOs/              # Contratos de entrada/salida de la API
│   ├── Interfaces/        # Contratos de servicios
│   ├── Services/          # Lógica de negocio
│   └── Exceptions/        # Excepciones de dominio personalizadas
├── Domain/                # Entidades EF Core (POCO)
├── Infrastructure/
│   ├── Data/              # ApplicationDbContext
│   ├── Interfaces/        # Contratos de repositorios
│   ├── Repositories/      # Implementaciones EF Core
│   └── Migrations/
└── Middleware/            # ExceptionHandlingMiddleware (manejo global de excepciones)
```

Un único `ApplicationDbContext` para todo el dominio (no hay separación por schema/módulo). `IGenericRepository<T>` / `GenericRepository<T>` cubre el CRUD estándar; cuando una entidad necesita queries propias, se define un repositorio específico (`IXxxRepository`/`XxxRepository`) que normalmente extiende o compone el genérico. `IUnitOfWork` centraliza `SaveChangesAsync` para que un Service pueda tocar varios repositorios en una sola transacción lógica.

### Patrón de un nuevo recurso

Para agregar un recurso nuevo seguir el patrón existente: `Domain/<Entidad>.cs` → `Infrastructure/Interfaces/I<Entidad>Repository.cs` + `Infrastructure/Repositories/<Entidad>Repository.cs` → `Business/Interfaces/I<Entidad>Service.cs` + `Business/Services/<Entidad>Service.cs` → `Business/DTOs/<Entidad>/` → `Controllers/<Entidad>Controller.cs`, y registrar repositorio + servicio en `Program.cs` (`AddScoped`).

### Autenticación y autorización

ASP.NET Core Identity (`IdentityCore<User>` con `IdentityRole<int>`) + JWT Bearer. El claim de rol se mapea con `RoleClaimType = "role"`. Hoy hay un único rol (`Admin`), seedeado en `Program.cs` al iniciar la app (busca/crea el rol y se lo asigna al usuario `ajazmatie` si no lo tiene). Rate limiting fijo (`AddFixedWindowLimiter`) aplicado al login: 10 requests/minuto, sin cola.

### CORS

Política `FrontendPolicy` en `Program.cs` con origins explícitos (`http://localhost:4200` dev, `https://jazfinanzaswebapp.azurestaticapps.net` producción). Agregar cualquier dominio nuevo de frontend ahí.

### Tests

xUnit + Moq + FluentAssertions, en `JazFinanzasApp.Tests/Services/`. Cubren los servicios con lógica de negocio relevante (`AuthService`, `TransactionService`, `CardTransactionService`, `InvestmentTransactionService`, `ReportService`). Al agregar lógica de negocio no trivial a un servicio nuevo o existente, sumar su test correspondiente siguiendo el mismo patrón (mock de repositorios/`IUnitOfWork`, sin DB real).

### Migraciones EF Core

Se aplican automáticamente al iniciar la app (`db.Database.MigrateAsync()` en `Program.cs`, dentro de un try/catch que solo loguea el error — no rompe el arranque si falla). Al crear una migración nueva, correr `dotnet ef database update` contra la base de dev antes de dar el cambio por terminado, para no dejar migraciones pendientes sin aplicar.

## Deploy

GitHub Actions, workflow `master_jazfinanzasappapi*.yml` (trigger en push a `master`), deploya a Azure App Service.
