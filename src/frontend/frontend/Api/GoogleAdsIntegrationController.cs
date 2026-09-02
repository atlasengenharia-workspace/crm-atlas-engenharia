using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Integracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CrmAtlas.Web.Api;

[ApiController, Authorize, Route("api/google-ads")]
public sealed class GoogleAdsIntegrationController(IGoogleAdsIntegrationService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<GoogleAdsIntegrationDto>> List(
        [FromQuery] GoogleAdsIntegrationFilter? filter,
        CancellationToken cancellationToken) =>
        service.ListAsync(filter, cancellationToken);

    [HttpGet("{id:long}")]
    public Task<GoogleAdsIntegrationDto> Get(long id, CancellationToken cancellationToken) =>
        service.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<GoogleAdsIntegrationDto>> Create(
        GoogleAdsIntegrationDto dto,
        CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public Task<GoogleAdsIntegrationDto> Update(long id, GoogleAdsIntegrationDto dto, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, dto, cancellationToken);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/auth-url")]
    public async Task<ActionResult<string>> GetAuthorizationUrl(
        long id,
        [FromQuery] string redirectUri,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        var url = await service.GetAuthorizationUrlAsync(id, redirectUri, state, cancellationToken);
        return Ok(url);
    }

    [HttpPost("{id:long}/auth")]
    public async Task<GoogleAdsIntegrationDto> SaveRefreshToken(
        long id,
        [FromBody] GoogleAdsAuthCodeRequest request,
        CancellationToken cancellationToken)
    {
        return await service.SaveRefreshTokenAsync(id, request.Code, request.RedirectUri, cancellationToken);
    }

    [HttpPost("{id:long}/sync")]
    public async Task<GoogleAdsIntegrationDto> Sync(long id, CancellationToken cancellationToken)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "sistema";
        return await service.SyncAsync(id, actor, cancellationToken);
    }

    [HttpPost("{id:long}/test")]
    public async Task<GoogleAdsIntegrationDto> Test(long id, CancellationToken cancellationToken)
    {
        return await service.TestAsync(id, cancellationToken);
    }

    [HttpGet("{id:long}/dashboard")]
    public Task<GoogleAdsDashboardSummary> Dashboard(long id, CancellationToken cancellationToken) =>
        service.GetDashboardSummaryAsync(id, cancellationToken);

    [HttpGet("{id:long}/campaigns")]
    public Task<IReadOnlyList<GoogleAdsCampaignDto>> Campaigns(long id, CancellationToken cancellationToken) =>
        service.ListCampaignsAsync(id, cancellationToken);

    [HttpGet("{id:long}/metrics")]
    public Task<IReadOnlyList<GoogleAdsMetricDto>> Metrics(
        long id,
        [FromQuery] DateOnly? start,
        [FromQuery] DateOnly? end,
        CancellationToken cancellationToken)
    {
        var endDate = end ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = start ?? endDate.AddDays(-30);
        return service.ListMetricsAsync(id, startDate, endDate, cancellationToken);
    }

    [HttpGet("{id:long}/leads")]
    public Task<IReadOnlyList<GoogleAdsLeadDto>> Leads(long id, CancellationToken cancellationToken) =>
        service.ListLeadsAsync(id, cancellationToken);

    public sealed record GoogleAdsAuthCodeRequest(string Code, string RedirectUri);
}
