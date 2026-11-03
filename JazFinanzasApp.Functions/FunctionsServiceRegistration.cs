using JazFinanzasApp.API.Infrastructure;
using JazFinanzasApp.API.Domain;
using JazFinanzasApp.API.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JazFinanzasApp.Functions
{
    // El grafo de dependencias de la Function App (plan-devoluciones-llm-v2.md, D6): el mismo registro de servicios que la API
    // (AddApplicationServices), así la devolución preparada de madrugada y la generada a pedido salen del mismo código. Lo único
    // propio es la base de datos. No corre migraciones: eso queda en el arranque de la API.
    public static class FunctionsServiceRegistration
    {
        public const string ConnectionStringName = "JazFinanzasAppConnectionString";

        // `configureDatabase` existe para los tests (base en memoria); por defecto usa SQL Server con la connection string de la
        // configuración (en Azure, la variable ConnectionStrings__JazFinanzasAppConnectionString).
        public static IServiceCollection AddFunctionsServices(
            this IServiceCollection services, IConfiguration configuration, Action<DbContextOptionsBuilder>? configureDatabase = null)
        {
            if (configureDatabase == null)
            {
                var connectionString = configuration.GetConnectionString(ConnectionStringName);
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException($"Falta la connection string '{ConnectionStringName}' (variable ConnectionStrings__{ConnectionStringName}).");
                configureDatabase = options => options.UseSqlServer(connectionString, sql => sql.CommandTimeout(120));
            }

            services.AddDbContext<ApplicationDbContext>(configureDatabase);
            services.AddApplicationServices(configuration);

            // Lo que Program.cs de la API registra por fuera de AddApplicationServices y los repositorios esperan: Identity (el repositorio de
            // usuarios pide UserManager) y el acceso al contexto HTTP. La función no autentica a nadie; esto solo completa el grafo.
            services.AddHttpContextAccessor();
            services.AddIdentityCore<User>()
                .AddRoles<IdentityRole<int>>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            return services;
        }
    }
}
