using CrmAtlas.ApplicationCore.Common;

namespace CrmAtlas.ApplicationCore.Documentos;

public sealed class PdfTemplate : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
