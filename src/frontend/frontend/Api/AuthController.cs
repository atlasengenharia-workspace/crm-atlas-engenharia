using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmAtlas.Web.Api;

[ApiController]
public sealed class AuthController : ControllerBase
{
    [HttpGet]
    [Route("login")]
    [Route("account/login")]
    [Route("auth0/login")]
    public async Task Login([FromQuery] string? returnUrl = "/")
    {
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);

        if (User.Identity?.IsAuthenticated == true)
        {
            Response.Redirect(safeReturnUrl);
            return;
        }

        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(safeReturnUrl)
            .WithParameter("ui_locales", "pt-BR")
            .Build();

        await HttpContext.ChallengeAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties);
    }

    [HttpGet]
    [Route("signup")]
    [Route("account/signup")]
    [Route("auth0/signup")]
    public async Task Signup([FromQuery] string? returnUrl = "/")
    {
        var safeReturnUrl = GetSafeReturnUrl(returnUrl);

        if (User.Identity?.IsAuthenticated == true)
        {
            Response.Redirect(safeReturnUrl);
            return;
        }

        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(safeReturnUrl)
            .WithParameter("screen_hint", "signup")
            .WithParameter("ui_locales", "pt-BR")
            .Build();

        await HttpContext.ChallengeAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties);
    }

    [HttpGet]
    [Authorize]
    [Route("logout")]
    [Route("account/logout")]
    [Route("auth0/logout")]
    public async Task Logout()
    {
        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri(Url.Content("~/"))
            .Build();

        await HttpContext.SignOutAsync(
            Auth0Constants.AuthenticationScheme,
            authenticationProperties);
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet]
    [Route("auth0")]
    [Route("auth0/profile")]
    public IActionResult Auth0Info()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Redirect("/login");
        }

        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var name = User.Identity.Name ?? User.FindFirst("name")?.Value ?? "Usuário";
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;
        var picture = User.FindFirst("picture")?.Value;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

        return Ok(new
        {
            provider = "Auth0",
            authenticated = true,
            user = new
            {
                id = userId,
                name,
                email,
                picture
            },
            claimsCount = claims.Count,
            claims
        });
    }

    private string GetSafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
}
