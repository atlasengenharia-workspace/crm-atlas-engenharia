namespace CrmAtlas.ApplicationCore.N8n;

public sealed class N8nOptions
{
    public string WebhookUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? IncomingSecret { get; set; }
}
