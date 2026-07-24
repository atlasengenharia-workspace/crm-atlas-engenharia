using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrmAtlas.Web.Pages;

public sealed class LoginModel : PageModel
{
    public string ReturnUrl { get; private set; } = "/";

    public IActionResult OnGet(string? returnUrl = "/")
    {
        ReturnUrl = GetSafeReturnUrl(returnUrl);

        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(ReturnUrl);
        }

        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
        return Page();
    }

    public async Task<IActionResult> OnGetChallengeAsync(string? returnUrl = "/")
    {
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);

        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(safeReturnUrl);
        }

        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(safeReturnUrl)
            .WithParameter("ui_locales", "pt-BR")
            .Build();

        await HttpContext.ChallengeAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties);

        return new EmptyResult();
    }

    private string GetSafeReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
}

