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

### QuotePrice: obligatorio en toda transacción

**Ninguna `Transaction` se persiste con `QuotePrice` en null**, sin importar el `MovementType` (incluidas las `EX` de transferencia interna) ni si la creó el usuario o un proceso automático. Los reportes convierten a la moneda de referencia dividiendo por ese campo: un null hace que la fila se descarte en silencio en los resúmenes mensuales (`t.QuotePrice.Value` sobre NULL en SQL) o que se cuente como si el monto ya estuviera en dólares en el reporte de Viajes (que hace `?? 1`).

Para resolverlo se usa `IQuotePriceResolver` (`Business/Services/QuotePriceResolver.cs`), inyectado en todo servicio que cree transacciones: USD devuelve 1, ARS usa la cotización `BLUE` de la fecha y el resto la `NA`. Cuando la fecha exacta no tiene cotización, `AssetQuoteRepository.GetQuotePrice` ya toma automáticamente la del día anterior más cercano — no hace falta manejar ese caso en el servicio.

`TransactionService` mantiene su propio `ResolveQuotePriceAsync` porque además acepta una cotización explícita enviada por el usuario; cuando no viene, cae en el mismo criterio.

### Total de un Viaje: dos fuentes disjuntas

El `Total` de `trips-general` y `trip-detail` suma dos cosas que no se pisan: **(1)** la parte propia de cada movimiento de los Eventos Compartidos vinculados al viaje (`Shares` con `PersonId == null`) y **(2)** los egresos etiquetados con `TripId` que no estén ya representados por (1). Sin (2), un viaje sin ningún Evento medía cero y los gastos enteramente propios se perdían.

La exclusión de (2) vive en el **reporte**, no en el dato: `GetTripOwnExpenseTransactionsAsync` / `GetTripOwnExpenseCardTransactionsAsync` descartan lo que sea respaldo de un movimiento (`SharedEventMovements.TransactionId` / `.CardTransactionId`) o lo haya creado el motor de pagos (columnas `Created*TransactionId` de `SharedEventPaymentAllocations`). Las transacciones `(Evento: <nombre>) <descripción>` que genera saldar una deuda **siguen etiquetadas con `TripId`** a propósito — son movimientos reales de las cuentas y tienen sentido en la lista del viaje; que un etiquetado de más no rompa el total es justamente el punto.

Dos convenciones de datos:

- **Un gasto de tarjeta se etiqueta siempre en el `CardTransaction`, nunca en una cuota.** El gasto se computa por devengado desde el consumo, y `TransactionService.EditTransactionAsync` y `TripService` rechazan asociar un viaje a una cuota (por `CardTransactionId` o por el prefijo `"(Tarjeta | "` de `TripMovementRules`). Cuando el Evento trackea las cuotas por separado, el reporte descarta el consumo padre por su cuenta — ver abajo.
- **La categoría es la real, no "Viajes".** El desglose de `trip-detail` agrupa por categoría: mandar todo a "Viajes" lo aplasta. "Viajes" queda para pasajes y alojamiento. Al crear movimientos de Evento hay que ponerles la categoría correcta desde el alta, porque `ApplyDebtAsync` la copia a las transacciones de liquidación que genera.

### Tarjetas: el consumo y sus cuotas

Un gasto de tarjeta vive en dos registros que **no dicen lo mismo**:

- El **`CardTransaction`** es la compra: fecha y monto reales del gasto. Existe desde que se carga.
- Las **`Transactions` de cuota** las crea `RegisterCardPaymentAsync` recién al pagar el resumen, fechadas en el mes del resumen (no el del gasto), en pesos aunque la compra sea en dólares, y ya netas de reintegros y descuentos.

Por eso todo lo que quiera medir *cuánto costó algo* lee `CardTransactions`, y las cuotas son flujo de caja. `Transaction.CardTransactionId` ata una cuota con su compra; la convención del detalle `(Tarjeta | n/m) <detalle>` es solo un rótulo y **no sirve para deducir la relación** — el detalle se puede editar de los dos lados.

Las filas que se agregan a mano al pagar (gastos que nunca se cargaron como consumo) llegan con `CardTransactionId == 0` y **crean su propio `CardTransaction`** de una cuota, devengado en el mes que se paga. Antes quedaban como cuotas huérfanas, imposibles de etiquetar a un viaje o de ver en las estadísticas de tarjeta.

`NetBreakdown` sigue siendo solo la fuente (1) — los gastos propios no pertenecen a ningún evento — así que `Total` puede ser mayor que la suma de sus filas.

### Tests

xUnit + Moq + FluentAssertions, en `JazFinanzasApp.Tests/Services/`. Cubren los servicios con lógica de negocio relevante (`AuthService`, `TransactionService`, `CardTransactionService`, `InvestmentTransactionService`, `ReportService`). Al agregar lógica de negocio no trivial a un servicio nuevo o existente, sumar su test correspondiente siguiendo el mismo patrón (mock de repositorios/`IUnitOfWork`, sin DB real).

### Migraciones EF Core

Se aplican automáticamente al iniciar la app (`db.Database.MigrateAsync()` en `Program.cs`, dentro de un try/catch que solo loguea el error — no rompe el arranque si falla). Al crear una migración nueva, correr `dotnet ef database update` contra la base de dev antes de dar el cambio por terminado, para no dejar migraciones pendientes sin aplicar.

## Deploy

GitHub Actions, workflow `master_jazfinanzasappapi*.yml` (trigger en push a `master`), deploya a Azure App Service.
