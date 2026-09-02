using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.Infrastructure.Common;
using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Servicos;
using CrmAtlas.ApplicationCore.Operacao;
using CrmAtlas.ApplicationCore.IA;
using CrmAtlas.ApplicationCore.Integracoes;
using CrmAtlas.Infrastructure.IA;
using CrmAtlas.Infrastructure.Integracoes;
using CrmAtlas.Infrastructure.Integrations;
using CrmAtlas.Infrastructure.Files;
using CrmAtlas.Infrastructure.Documents;
using CrmAtlas.Infrastructure.Imports;
using CrmAtlas.ApplicationCore.Identidade;
using CrmAtlas.ApplicationCore.Dashboard;
using CrmAtlas.ApplicationCore.Sistema;
using CrmAtlas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrmAtlas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        services.TryAddSingleton<IRealtimeNotifier, NullRealtimeNotifier>();
        services.TryAddScoped<IUserAccessor, NullUserAccessor>();
        services.AddSingleton<RealtimeSaveChangesInterceptor>();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = ConvertPostgresUrlToConnectionString(connectionString);
            }

            services.AddDbContext<AtlasDbContext>(
                (provider, options) => options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AtlasDbContext).Assembly.FullName);
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .AddInterceptors(provider.GetRequiredService<RealtimeSaveChangesInterceptor>()),
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Singleton);
        }
        else
        {
            services.AddDbContext<AtlasDbContext>(
                (provider, options) => options.UseNpgsql("Host=localhost;Database=atlas_dummy", npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AtlasDbContext).Assembly.FullName);
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .AddInterceptors(provider.GetRequiredService<RealtimeSaveChangesInterceptor>()),
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Singleton);
        }

        // Blazor Server components live for the whole circuit. Database services
        // must not share one scoped DbContext across pages/layouts and events.
        services.AddTransient(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddTransient<ICadastroServicoRepository, CadastroServicoRepository>();
        services.AddTransient<IClienteService, ClienteService>();
        services.AddTransient<ICondicaoPagamentoService, CondicaoPagamentoService>();
        services.AddTransient<ICadastroServicoService, CadastroServicoService>();
        services.AddTransient<IServicoTipoCampoConfigService, ServicoTipoCampoConfigService>();
        services.AddTransient<ICustoIndiretoService, CustoIndiretoService>();
        services.AddTransient<ILancamentoService, LancamentoService>();
        services.AddTransient<IOrcamentoService, OrcamentoService>();
        services.AddTransient<IPrestadorService, PrestadorService>();
        services.AddTransient<IAcompanhamentoService, AcompanhamentoService>();
        services.AddTransient<IAcompanhamentoRepository, AcompanhamentoRepository>();
        services.AddTransient<IAcompanhamentoReportService, AcompanhamentoPdfReportService>();
        services.AddTransient<IAcompanhamentoSpreadsheetReader, AcompanhamentoSpreadsheetReader>();
        services.AddTransient<IAtlasWorkbookImportService, AtlasWorkbookImportService>();
        services.AddTransient<INotificationService, NotificationService>();
        services.AddTransient<IIdentityService, IdentityService>();
        services.AddTransient<IUserPreferencesService, UserPreferencesService>();
        services.AddTransient<IGlobalSearchService, GlobalSearchService>();
        services.AddTransient<IDashboardQueryService, DashboardQueryService>();
        services.AddTransient<ISistemaAtualizacaoService, SistemaAtualizacaoService>();
        services.AddTransient<IGoogleAdsIntegrationService, GoogleAdsIntegrationService>();
        services.AddTransient<IGoogleAdsApiClient, GoogleAdsApiClient>();
        services.AddTransient<IContextRetriever, AtlasAiContextRetriever>();
        services.AddTransient<OpenAiLlmClient>();
        services.AddTransient<HuggingFaceLlmClient>();
        services.AddTransient<ILlmClient, ProviderBasedLlmClient>();
        services.AddTransient<IAtlasAiService, AtlasAiService>();
        services.Configure<AtlasAiOptions>(configuration.GetSection("AI"));
        services.AddScoped<IReceiptStorage, LocalReceiptStorage>();
        services.AddHttpClient<ICepLookupService, ViaCepLookupService>(client =>
        {
            client.BaseAddress = new Uri("https://viacep.com.br/ws/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    private static string ConvertPostgresUrlToConnectionString(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = uri.AbsolutePath.TrimStart('/');
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Channel Binding=Require";
    }
}
