using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;
using CrmAtlas.ApplicationCore.Identidade;

namespace CrmAtlas.ApplicationCore.Notificacoes;

public sealed class Notification : Entity
{
    public long UserId { get; set; }
    public Usuario User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public NotificationCategory Category { get; set; }
    public NotificationServiceType? ServiceType { get; set; }
    public decimal? Amount { get; set; }
    public NotificationRuleType? RuleType { get; set; }
    public string? ReferenceKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastActive { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}

public sealed class NotificationRule : Entity
{
    public string Name { get; set; } = string.Empty;
    public NotificationRuleType Type { get; set; }
    public NotificationCategory Category { get; set; }
    public int DaysThreshold { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
