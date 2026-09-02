using System.ComponentModel.DataAnnotations;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Integracoes;

public sealed record GoogleAdsIntegrationDto(
    long? Id,
    [Required] string Key,
    [Required] string Name,
    string Description,
    GoogleIntegrationStatus Status,
    string? DeveloperToken,
    string? ClientId,
    string? ClientSecret,
    string? RefreshToken,
    string? LoginCustomerId,
    bool AutoSync,
    int SyncIntervalMin,
    bool ImportLeadsAsBudgets,
    bool CreateFinancialEntries,
    DateTimeOffset? LastSyncAt,
    string? ErrorMessage);

public sealed record GoogleAdsIntegrationFilter(
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    string? SortKey = null,
    bool SortDescending = false);

public sealed record GoogleAdsCampaignDto(
    long Id,
    long IntegrationId,
    long ExternalCampaignId,
    string Name,
    string Status,
    string? Channel,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal? BudgetAmount,
    long Leads,
    decimal Spend,
    DateTimeOffset SyncedAt);

public sealed record GoogleAdsMetricDto(
    long Id,
    long CampaignId,
    string CampaignName,
    DateOnly Date,
    decimal Cost,
    long Clicks,
    long Impressions,
    double Conversions,
    double ConversionsValue,
    double AllConversions,
    double Ctr,
    double Cpm,
    double Cpc,
    DateTimeOffset SyncedAt);

public sealed record GoogleAdsLeadDto(
    long Id,
    long IntegrationId,
    long CampaignId,
    string CampaignName,
    DateTimeOffset SubmittedAt,
    string? GclId,
    string? Name,
    string? Email,
    string? Phone,
    string? Message,
    bool ConvertedToBudget,
    long? OrcamentoId,
    DateTimeOffset? ConvertedAt);

public sealed record GoogleAdsDashboardSummary(
    decimal TotalCost,
    long TotalClicks,
    long TotalImpressions,
    double TotalConversions,
    decimal Cpl,
    double AvgCtr,
    decimal AvgCpm,
    decimal AvgCpc,
    int ActiveCampaigns,
    int TotalLeads,
    int PendingLeads);
