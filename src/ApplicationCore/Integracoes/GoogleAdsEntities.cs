using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Integracoes;

public sealed class GoogleAdsIntegration : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GoogleIntegrationStatus Status { get; set; } = GoogleIntegrationStatus.DISCONNECTED;
    public string? DeveloperToken { get; set; }
    public string? ClientId { get; set; }
    public string? EncryptedClientSecret { get; set; }
    public string? EncryptedRefreshToken { get; set; }
    public string? LoginCustomerId { get; set; }
    public bool AutoSync { get; set; }
    public int SyncIntervalMin { get; set; } = 60;
    public DateTimeOffset? LastSyncAt { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ImportLeadsAsBudgets { get; set; } = true;
    public bool CreateFinancialEntries { get; set; } = true;
    public ICollection<GoogleAdsCampaign> Campaigns { get; set; } = [];
    public ICollection<GoogleAdsCampaignMetric> Metrics { get; set; } = [];
    public ICollection<GoogleAdsLead> Leads { get; set; } = [];
    public ICollection<GoogleAdsIntegrationAudit> Audits { get; set; } = [];
}

public sealed class GoogleAdsCampaign : Entity
{
    public long IntegrationId { get; set; }
    public GoogleAdsIntegration Integration { get; set; } = null!;
    public long ExternalCampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public long? BudgetAmountMicros { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
}

public sealed class GoogleAdsCampaignMetric : Entity
{
    public long IntegrationId { get; set; }
    public GoogleAdsIntegration Integration { get; set; } = null!;
    public long CampaignId { get; set; }
    public GoogleAdsCampaign Campaign { get; set; } = null!;
    public DateOnly Date { get; set; }
    public long CostMicros { get; set; }
    public long Clicks { get; set; }
    public long Impressions { get; set; }
    public double Conversions { get; set; }
    public double ConversionsValue { get; set; }
    public double AllConversions { get; set; }
    public double Ctr { get; set; }
    public double Cpm { get; set; }
    public double Cpc { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
}

public sealed class GoogleAdsLead : Entity
{
    public long IntegrationId { get; set; }
    public GoogleAdsIntegration Integration { get; set; } = null!;
    public long CampaignId { get; set; }
    public GoogleAdsCampaign Campaign { get; set; } = null!;
    public DateTimeOffset SubmittedAt { get; set; }
    public string? GclId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Message { get; set; }
    public bool ConvertedToBudget { get; set; }
    public long? OrcamentoId { get; set; }
    public DateTimeOffset? ConvertedAt { get; set; }
}

public sealed class GoogleAdsIntegrationAudit : Entity
{
    public long IntegrationId { get; set; }
    public GoogleAdsIntegration Integration { get; set; } = null!;
    public GoogleIntegrationAction Action { get; set; }
    public GoogleIntegrationStatus ResultStatus { get; set; }
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Message { get; set; }
}
