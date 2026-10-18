# JazFinanzasApp.Backend

API REST de JazFinanzasApp — finanzas personales e inversiones. Maneja autenticación, cuentas, tarjetas de crédito, transacciones, inversiones (acciones, criptomonedas, bonos con cronograma y cobros de amortización e interés, portfolios), gastos compartidos y un asistente con IA que cuenta en castellano cómo vino el mes.

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
| `Anthropic:ApiKey` | Clave de la API de Anthropic para el asistente (`Anthropic__ApiKey` en Azure). Si falta, el backend levanta igual y el asistente responde que no está configurado. **Nunca en el repo ni en `appsettings*.json`.** | Azure App Settings / variable de entorno local |
| `Assistant:Model`, `Assistant:MaxTokens`, `Assistant:Prices` | Modelo, techo de tokens de salida y precios por millón de tokens (alimentan el costo y el tope mensual) | `appsettings.json` |
| `AssistantAlerts:*` | Umbrales de las alertas por reglas de la bandeja de Inicio (categoría que se dispara, egreso grande, ingreso que no llegó) | `appsettings.json` |

> **Importante:** Nunca subir credenciales reales al repositorio. Usar variables de entorno o Azure Key Vault en producción.

### Asistente (devoluciones con IA)

Lee los números que los reportes ya calculan y los cuenta en castellano; **no calcula ni carga nada**. Vive en `Business/Assistant/`: fuentes de contexto (una por área de la app), cliente del modelo detrás de una interfaz, instrucciones versionadas como archivos `.md`, control de cifras y registro de gasto con tope mensual. Es por usuario: solo lo ve quien tiene una fila habilitada en `AssistantUserSettings`.

- **Endpoints** (`AssistantController`): `status`, `reviews` (historial, última, detalle, generar, calificar, `preview` solo `Admin`) y `findings`.
- **Alertas por reglas** en la bandeja de pendientes (`CategorySpike`, `LargeMovement`, `MissingIncome`): para todos los usuarios y sin costo.
- **Operación:** cuenta, claves y topes en `docs/plans/guias/servicio-anthropic.md`; revisión de calidad en `docs/plans/guias/revision-asistente.md`. Los tests nunca llaman a la API real; hay tests manuales (`Tests/Manual/`) que sí lo hacen y solo corren si se los pide.

---

## Tests

```bash
dotnet test
```

Cubren los servicios principales: `AuthService`, `TransactionService`, `CardTransactionService`, `InvestmentTransactionService`, `ReportService`.

---

## Despliegue

Publicar el proyecto `JazFinanzasApp.API` a un Azure App Service configurando las connection strings y las variables JWT en *Configuration → Application settings* del portal. CI/CD automático vía GitHub Actions al pushear a `master`.
