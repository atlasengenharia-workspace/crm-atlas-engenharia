using Auth0.AspNetCore.Authentication;
using CrmAtlas.Infrastructure;
using CrmAtlas.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Radzen;
using CrmAtlas.Web.Components;
using System.Globalization;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.Web.Realtime;
using Microsoft.Extensions.DependencyInjection.Extensions;

var ptBr = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

var builder = WebApplication.CreateBuilder(args);

if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddMudServices();
builder.Services.AddRadzenComponents();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Replace(ServiceDescriptor.Singleton(typeof(SignalRRealtimeNotifier), typeof(SignalRRealtimeNotifier)));
builder.Services.Replace(ServiceDescriptor.Singleton<IRealtimeNotifier>(sp => sp.GetRequiredService<SignalRRealtimeNotifier>()));
builder.Services.AddSingleton<IRealtimeChangeFeed>(sp => sp.GetRequiredService<SignalRRealtimeNotifier>());
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 32 * 1024;
    options.MaximumParallelInvocationsPerClient = 1;
    options.StreamBufferCapacity = 10;
});

var auth0Domain = builder.Configuration["Auth0:Domain"] ?? string.Empty;
var auth0ClientId = builder.Configuration["Auth0:ClientId"] ?? string.Empty;
var auth0ClientSecret = builder.Configuration["Auth0:ClientSecret"] ?? string.Empty;

if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(auth0Domain) ||
     string.IsNullOrWhiteSpace(auth0ClientId) ||
     string.IsNullOrWhiteSpace(auth0ClientSecret)))
{
    throw new InvalidOperationException(
        "Configure Auth0__Domain, Auth0__ClientId e Auth0__ClientSecret.");
}

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = auth0Domain;
    options.ClientId = auth0ClientId;
    options.ClientSecret = auth0ClientSecret;
    options.Scope = "openid profile email";
    options.OpenIdConnectEvents = new OpenIdConnectEvents
    {
        OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/authentication-error");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);
builder.Services.AddServerSideBlazor(options => options.DetailedErrors = true);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CrmAtlas.Web.Api.ApiExceptionHandler>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE acompanhamento_servicos ADD COLUMN IF NOT EXISTS cnpj_cpf text;
                ALTER TABLE acompanhamento_servicos ADD COLUMN IF NOT EXISTS endereco text;
                ALTER TABLE acompanhamento_servicos ADD COLUMN IF NOT EXISTS nota_fiscal text;
                ALTER TABLE acompanhamento_servicos ADD COLUMN IF NOT EXISTS proxima_parcela date;
                ALTER TABLE acompanhamento_servicos ADD COLUMN IF NOT EXISTS proxima_parcela_texto text;
                ALTER TABLE acompanhamento_servico_situacao_config ADD COLUMN IF NOT EXISTS cor character varying(16);

                UPDATE acompanhamento_servico_situacao_config SET cor = CASE
                    WHEN lower(nome) LIKE 'agendado%' THEN '#C7D2FE'
                    WHEN lower(nome) LIKE 'aguar%contratante%' OR lower(nome) LIKE 'aguard%cliente%' THEN '#FDE68A'
                    WHEN lower(nome) LIKE 'aguar%document%' THEN '#FDBA74'
                    WHEN lower(nome) LIKE 'comunicado%' THEN '#FCA5A5'
                    WHEN lower(nome) LIKE 'concluído%aguar%pag%' OR lower(nome) LIKE 'concluido%aguar%pag%' THEN '#93C5FD'
                    WHEN lower(nome) LIKE 'concluído%' OR lower(nome) LIKE 'concluido%' THEN '#86EFAC'
                    WHEN lower(nome) LIKE 'em análise%' OR lower(nome) LIKE 'em analise%' THEN '#D1D5DB'
                    WHEN lower(nome) LIKE 'executar%' THEN '#C4B5FD'
                    WHEN lower(nome) LIKE '%vistoria%' THEN '#67E8F9'
                    ELSE '#BFDBFE'
                END WHERE cor IS NULL OR btrim(cor) = '';
            ");
            dbContext.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        // A exceção é engolida de propósito para a aplicação subir mesmo com o
        // banco fora de sincronia. O efeito colateral é que uma migração que
        // falha não aparece em lugar nenhum além do log — se uma coluna nova
        // some ("42703: column ... does not exist"), a causa está aqui.
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao aplicar migrações do banco de dados.");
    }
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapControllers();
app.MapHub<CrmRealtimeHub>("/hubs/crm").RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
