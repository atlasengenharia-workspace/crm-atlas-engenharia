using Auth0.AspNetCore.Authentication;
using CrmAtlas.Infrastructure;
using CrmAtlas.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using CrmAtlas.Web.Components;

var builder = WebApplication.CreateBuilder(args);

if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddMudServices();
builder.Services.AddInfrastructure(builder.Configuration);

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
    .AddInteractiveServerComponents();

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
            ");
            dbContext.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
