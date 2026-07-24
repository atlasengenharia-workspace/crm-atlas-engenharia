using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Servicos;
using CrmAtlas.ApplicationCore.Operacao;
using CrmAtlas.Infrastructure.Integrations;
using CrmAtlas.Infrastructure.Files;
using CrmAtlas.Infrastructure.Documents;
using CrmAtlas.Infrastructure.Imports;
using CrmAtlas.ApplicationCore.Identidade;
using CrmAtlas.ApplicationCore.Dashboard;
using CrmAtlas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrmAtlas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<AtlasDbContext>(
                options => options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(AtlasDbContext).Assembly.FullName)),
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Singleton);
            // Blazor Server components live for the whole circuit. Database services
            // must not share one scoped DbContext across pages/layouts and events.
            services.AddTransient(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddTransient<ICadastroServicoRepository, CadastroServicoRepository>();
            services.AddTransient<IClienteService, ClienteService>();
            services.AddTransient<ICondicaoPagamentoService, CondicaoPagamentoService>();
            services.AddTransient<ICadastroServicoService, CadastroServicoService>();
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
            services.AddScoped<IReceiptStorage, LocalReceiptStorage>();
            services.AddHttpClient<ICepLookupService, ViaCepLookupService>(client =>
            {
                client.BaseAddress = new Uri("https://viacep.com.br/ws/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });
        }

        return services;
    }
}
