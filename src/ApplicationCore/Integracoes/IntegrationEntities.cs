using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Integracoes;

public sealed class GoogleIntegration : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GoogleIntegrationStatus Status { get; set; } = GoogleIntegrationStatus.DISCONNECTED;
    public string? AccountEmail { get; set; }
    public ICollection<GoogleIntegrationScope> Scopes { get; set; } = [];
    public string? ClientId { get; set; }
    public string? EncryptedClientSecret { get; set; }
    public string? RedirectUri { get; set; }
    public bool WebhookEnabled { get; set; }
    public bool AutoSync { get; set; }
    public int SyncIntervalMin { get; set; } = 60;
    public DateTimeOffset? LastSyncAt { get; set; }
    public string? ErrorMessage { get; set; }
    public ICollection<GoogleIntegrationAudit> Audits { get; set; } = [];
    public ICollection<GoogleSheetReportMetadata> Reports { get; set; } = [];
}

public sealed class GoogleIntegrationScope
{
    public long IntegrationId { get; set; }
    public GoogleIntegration Integration { get; set; } = null!;
    public string Scope { get; set; } = string.Empty;
}

public sealed class GoogleIntegrationAudit : Entity
{
    public long IntegrationId { get; set; }
    public GoogleIntegration Integration { get; set; } = null!;
    public GoogleIntegrationAction Action { get; set; }
    public GoogleIntegrationStatus ResultStatus { get; set; }
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Message { get; set; }
}

public sealed class GoogleSheetReportMetadata : Entity
{
    public long IntegrationId { get; set; }
    public GoogleIntegration Integration { get; set; } = null!;
    public string ReportName { get; set; } = string.Empty;
    public string SpreadsheetId { get; set; } = string.Empty;
    public string SpreadsheetUrl { get; set; } = string.Empty;
    public string? UpdatedRange { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
}

public sealed class WhatsAppMetaIntegration : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WhatsAppMetaIntegrationStatus Status { get; set; } = WhatsAppMetaIntegrationStatus.DISCONNECTED;
    public string? PhoneNumberId { get; set; }
    public string? BusinessAccountId { get; set; }
    public string? EncryptedPermanentToken { get; set; }
    public string? EncryptedWebhookVerifyToken { get; set; }
    public bool WebhookEnabled { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
    public string? ErrorMessage { get; set; }
    public ICollection<WhatsAppMetaIntegrationAudit> Audits { get; set; } = [];
}

public sealed class WhatsAppMetaIntegrationAudit : Entity
{
    public long IntegrationId { get; set; }
    public WhatsAppMetaIntegration Integration { get; set; } = null!;
    public WhatsAppMetaIntegrationAction Action { get; set; }
    public WhatsAppMetaIntegrationStatus ResultStatus { get; set; }
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Message { get; set; }
}
