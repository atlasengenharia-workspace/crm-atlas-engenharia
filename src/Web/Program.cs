using Auth0.AspNetCore.Authentication;
using CrmAtlas.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using MudBlazor.Services;
using CrmAtlas.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();
builder.Services.AddInfrastructure(builder.Configuration);

var auth0Domain = builder.Configuration["Auth0:Domain"] ?? string.Empty;
var auth0ClientId = builder.Configuration["Auth0:ClientId"] ?? string.Empty;
var auth0ClientSecret = builder.Configuration["Auth0:ClientSecret"] ?? string.Empty;

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

var app = builder.Build();

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
