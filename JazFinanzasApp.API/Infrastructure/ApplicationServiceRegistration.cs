using Microsoft.Extensions.DependencyInjection.Extensions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.Assistant.Sources;
using JazFinanzasApp.API.Business.Interfaces;
using JazFinanzasApp.API.Business.Services;
using JazFinanzasApp.API.Infrastructure.Interfaces;
using JazFinanzasApp.API.Infrastructure.Repositories;

namespace JazFinanzasApp.API.Infrastructure
{
    // El registro de repositorios y servicios, fuera de Program.cs para poder armar el mismo grafo de
    // dependencias sin levantar la aplicación: el arranque de Program.cs aplica las migraciones pendientes
    // sobre la base configurada, y una medición o un test manual contra datos reales no puede disparar eso
    // (plan-devoluciones-llm.md, Fase 4). Program.cs sigue registrando lo demás: DbContext, Identity, JWT,
    // CORS y rate limiting.
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
        // Registrar los repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ITransactionClassRepository, TransactionClassRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<IAccount_AssetTypeRepository, Account_AssetTypeRepository>();
        services.AddScoped<IAssetTypeRepository, AssetTypeRepository>();
        services.AddScoped<IAsset_UserRepository, Asset_UserRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAssetQuoteRepository, AssetQuoteRepository>();
        services.AddScoped<ICardTransactionRepository, CardTransactionRepository>();
        services.AddScoped<ICardPaymentRepository, CardPaymentRepository>();
        services.AddScoped<IInvestmentTransactionRepository, InvestmentTransactionRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IAssetSplitEventRepository, AssetSplitEventRepository>();
        services.AddScoped<IBondPaymentRepository, BondPaymentRepository>();
        services.AddScoped<IBondCollectionRepository, BondCollectionRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<ISharedExpenseRepository, SharedExpenseRepository>();
        services.AddScoped<ICardTransactionDiscountRepository, CardTransactionDiscountRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<ITripSuggestionDismissalRepository, TripSuggestionDismissalRepository>();
        services.AddScoped<ISharedEventRepository, SharedEventRepository>();
        services.AddScoped<ISharedEventMovementRepository, SharedEventMovementRepository>();
        services.AddScoped<ISharedEventPaymentRepository, SharedEventPaymentRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IReportFavoriteRepository, ReportFavoriteRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Registrar los servicios
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IInvestmentTransactionService, InvestmentTransactionService>();
        services.AddScoped<IFiatCurrencyExchangeService, FiatCurrencyExchangeService>();
        services.AddScoped<ICardTransactionService, CardTransactionService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITransactionClassService, TransactionClassService>();
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<IAssetSplitEventService, AssetSplitEventService>();
        services.AddScoped<IBondCollectionService, BondCollectionService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IQuotePriceResolver, QuotePriceResolver>();
        services.AddScoped<ISharedExpenseService, SharedExpenseService>();
        services.AddScoped<ICardTransactionDiscountService, CardTransactionDiscountService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<ISharedEventService, SharedEventService>();
        services.AddScoped<ISharedEventPaymentService, SharedEventPaymentService>();
        services.AddScoped<ISharedEventImportService, SharedEventImportService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<INetWorthReportService, NetWorthReportService>();
        services.AddScoped<IIncomeExpenseReportService, IncomeExpenseReportService>();
        services.AddScoped<ICardReportService, CardReportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IInvestmentReportService, InvestmentReportService>();
        services.AddScoped<ITripReportService, TripReportService>();
        services.AddScoped<ISharedEventReportService, SharedEventReportService>();
        services.AddScoped<IReportFavoriteService, ReportFavoriteService>();
        services.Configure<AssistantOptions>(configuration.GetSection(AssistantOptions.SectionName));
        services.AddScoped<IAssistantCurrencyResolver, AssistantCurrencyResolver>();
        // Fuentes de contexto del asistente: sumar una es una clase y una línea acá (plan-devoluciones-llm.md, D2).
        services.AddScoped<IAssistantContextSource, IncomeExpenseContextSource>();
        services.AddScoped<IAssistantContextSource, TopExpensesContextSource>();
        services.AddScoped<IAssistantContextSource, CardsContextSource>();
        services.AddScoped<IAssistantContextSource, NetWorthContextSource>();
        services.AddScoped<IAssistantContextSource, InvestmentsContextSource>();
        services.AddScoped<IAssistantContextSource, TripsContextSource>();
        services.AddScoped<IAssistantContextSource, AlertsContextSource>();
        services.AddScoped<AssistantContextBuilder>();
        // Cliente del modelo y registro de gasto (plan-devoluciones-llm.md, D4 y D11). El cliente es singleton
        // porque guarda el cliente HTTP del SDK; crea la conexión recién en el primer uso, así que sin clave
        // el backend levanta igual.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAssistantModelClient, AnthropicAssistantModelClient>();
        services.AddScoped<IAssistantUsageRepository, AssistantUsageRepository>();
        services.AddScoped<IAssistantUsageService, AssistantUsageService>();
        services.AddScoped<IAssistantReviewRepository, AssistantReviewRepository>();
        services.AddScoped<IAssistantReviewService, AssistantReviewService>();

            return services;
        }
    }
}
