using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Enums;

namespace CrmAtlas.ApplicationCore.Identidade;

public sealed class Usuario : Entity
{
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Auth0Sub { get; set; }
    public string? PasswordHash { get; set; }
    public bool Enabled { get; set; }
    public string? VerificationCode { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public UserRole? Role { get; set; }
}

public sealed class UsuarioPreferencia : Entity
{
    public long UserId { get; set; }
    public Usuario User { get; set; } = null!;
    public string Theme { get; set; } = "Sistema";
    public string TableDensity { get; set; } = "Confortável";
    public bool SidebarOpen { get; set; } = true;
    public bool EmailSummary { get; set; } = true;
    public bool BrowserAlerts { get; set; }
    public DateTime UpdatedAt { get; set; }
}
