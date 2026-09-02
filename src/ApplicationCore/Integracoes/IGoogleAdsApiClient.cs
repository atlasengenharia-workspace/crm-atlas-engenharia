namespace CrmAtlas.ApplicationCore.Integracoes;

public sealed record GoogleAdsCampaignData(
    long ExternalId,
    string Name,
    string Status,
    string? Channel,
    DateOnly? StartDate,
    DateOnly? EndDate,
    long? BudgetAmountMicros);

public sealed record GoogleAdsMetricData(
    long ExternalCampaignId,
    string CampaignName,
    DateOnly Date,
    long CostMicros,
    long Clicks,
    long Impressions,
    double Conversions,
    double ConversionsValue,
    double AllConversions,
    double Ctr,
    double Cpm,
    double Cpc);

public sealed record GoogleAdsLeadData(
    long ExternalCampaignId,
    string CampaignName,
    DateTimeOffset SubmittedAt,
    string? GclId,
    string? Name,
    string? Email,
    string? Phone,
    string? Message);

public interface IGoogleAdsApiClient
{
    Task TestConnectionAsync(GoogleAdsIntegrationConfig config, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoogleAdsCampaignData>> GetCampaignsAsync(GoogleAdsIntegrationConfig config, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoogleAdsMetricData>> GetMetricsAsync(GoogleAdsIntegrationConfig config, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoogleAdsLeadData>> GetLeadsAsync(GoogleAdsIntegrationConfig config, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<string> BuildAuthorizationUrlAsync(GoogleAdsIntegrationConfig config, string redirectUri, string state, CancellationToken cancellationToken = default);
    Task<string> ExchangeCodeAsync(GoogleAdsIntegrationConfig config, string redirectUri, string code, CancellationToken cancellationToken = default);
}

public sealed record GoogleAdsIntegrationConfig(
    string DeveloperToken,
    string ClientId,
    string ClientSecret,
    string? RefreshToken,
    string? LoginCustomerId);
