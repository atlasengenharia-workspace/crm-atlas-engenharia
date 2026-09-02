using CrmAtlas.ApplicationCore.Common;
using Microsoft.AspNetCore.Components.Authorization;

namespace CrmAtlas.Web.Services;

public sealed class CircuitUserAccessor(AuthenticationStateProvider authenticationStateProvider) : IUserAccessor
{
    public async Task<string?> GetUserNameAsync(CancellationToken ct = default)
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.Name
            ?? state.User.FindFirst("name")?.Value
            ?? state.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    }
}
