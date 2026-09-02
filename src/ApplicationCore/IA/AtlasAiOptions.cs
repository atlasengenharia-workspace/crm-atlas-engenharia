namespace CrmAtlas.ApplicationCore.IA;

public sealed class AtlasAiOptions
{
    public string? Provider { get; set; }
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
    public string? OrgId { get; set; }
    public bool FallbackWhenNotConfigured { get; set; } = true;
}
