# JazFinanzasApp.Backend

API REST de JazFinanzasApp — finanzas personales e inversiones. Maneja autenticación, cuentas, tarjetas de crédito, transacciones, inversiones (acciones, criptomonedas, portfolios) y gastos compartidos.

---

## Arquitectura

```
┌─────────────────────────────────────────┐
│         ASP.NET Core 8 Web API          │  ← Azure App Service
│  Controllers → Services → Repositories │
└───────────────────┬─────────────────────┘
                    │ EF Core
┌───────────────────▼─────────────────────┐
│          Azure SQL Database             │
└─────────────────────────────────────────┘
```

| Capa | Responsabilidad |
|---|---|
| **Controllers** | Recibir/validar requests HTTP, delegar al servicio, devolver respuesta. |
| **Business / Services** | Lógica de negocio, orquestación, generación de tokens JWT. |
| **Business / DTOs** | Objetos de transferencia de datos entre capas y hacia el cliente. |
| **Infrastructure / Repositories** | Acceso a datos vía EF Core. Sin conocimiento de capas superiores. |
| **Domain** | Entidades del dominio (modelos EF Core). |

Detalle de estructura de carpetas y convenciones de implementación en [CLAUDE.md](./CLAUDE.md).

---

## Stack tecnológico

| Tecnología | Versión |
|---|---|
| .NET / ASP.NET Core | 8.0 |
| Entity Framework Core | 8.0 |
| ASP.NET Core Identity | 8.0 |
| JWT Bearer Authentication | 8.0 |
| SQL Server / Azure SQL | — |
| Swagger (Swashbuckle) | 6.4 |

### Tests

| Tecnología | Uso |
|---|---|
| xUnit | Framework de tests |
| Moq | Mocking de dependencias |
| FluentAssertions | Aserciones legibles |
| Coverlet | Cobertura de código |

---

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local o Azure SQL Database)

---

## Configuración y ejecución

1. **Configurar la cadena de conexión y JWT en `JazFinanzasApp.API/appsettings.Development.json`:**
   ```json
   {
     "ConnectionStrings": {
       "JazFinanzasAppConnectionString": "Server=localhost;Database=JazFinanzasAppDB;TrustServerCertificate=True;Trusted_Connection=True"
     },
     "Jwt": {
       "Key": "<clave-secreta-minimo-32-caracteres>",
       "Issuer": "https://localhost:7203",
       "Audience": "https://localhost:4200"
     }
   }
   ```

2. **Aplicar migraciones:**
   ```bash
   cd JazFinanzasApp.API
   dotnet ef database update
   ```

3. **Ejecutar la API:**
   ```bash
   dotnet run
   ```
   La API estará disponible en `https://localhost:7203`. Swagger UI en `https://localhost:7203/swagger`.

---

## Variables de entorno y configuración

| Variable | Descripción | Dónde configurar |
|---|---|---|
| `ConnectionStrings:JazFinanzasAppConnectionString` | Cadena de conexión a SQL Server | `appsettings.json` / Azure App Settings |
| `Jwt:Key` | Clave secreta para firmar tokens JWT (mín. 32 chars) | `appsettings.json` / Azure App Settings |
| `Jwt:Issuer` | URL del emisor del token | `appsettings.json` |
| `Jwt:Audience` | URL del receptor del token | `appsettings.json` |

> **Importante:** Nunca subir credenciales reales al repositorio. Usar variables de entorno o Azure Key Vault en producción.

---

## Tests

```bash
dotnet test
```

Cubren los servicios principales: `AuthService`, `TransactionService`, `CardTransactionService`, `InvestmentTransactionService`, `ReportService`.

---

## Despliegue

Publicar el proyecto `JazFinanzasApp.API` a un Azure App Service configurando las connection strings y las variables JWT en *Configuration → Application settings* del portal. CI/CD automático vía GitHub Actions al pushear a `master`.
