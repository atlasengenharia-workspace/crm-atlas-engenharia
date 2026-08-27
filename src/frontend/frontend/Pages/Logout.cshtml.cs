using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrmAtlas.Web.Pages;

[Authorize]
public sealed class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";

        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri(Url.Content("~/Login"))
            .Build();

        // Remove a sessão local antes de iniciar o redirecionamento externo.
        // Assim o usuário deixa de estar autenticado imediatamente, mesmo que
        // a comunicação com o provedor leve alguns instantes.
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties);

        return new EmptyResult();
    }
}
