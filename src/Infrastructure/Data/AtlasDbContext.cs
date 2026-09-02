using CrmAtlas.ApplicationCore.Acompanhamentos;
using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Common;
using CrmAtlas.ApplicationCore.Documentos;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Identidade;
using CrmAtlas.ApplicationCore.Integracoes;
using CrmAtlas.ApplicationCore.Notificacoes;
using CrmAtlas.ApplicationCore.Servicos;
using Microsoft.EntityFrameworkCore;

namespace CrmAtlas.Infrastructure.Data;

public sealed class AtlasDbContext(DbContextOptions<AtlasDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Avcb> Avcbs => Set<Avcb>();
    public DbSet<Clcb> Clcbs => Set<Clcb>();
    public DbSet<Obra> Obras => Set<Obra>();
    public DbSet<ProcessoAdm> ProcessosAdm => Set<ProcessoAdm>();
    public DbSet<CondicaoPagamento> CondicoesPagamento => Set<CondicaoPagamento>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<OrcamentoSituacao> OrcamentoSituacoes => Set<OrcamentoSituacao>();
    public DbSet<OrcamentoHistorico> OrcamentoHistoricos => Set<OrcamentoHistorico>();
    public DbSet<CadastroServico> CadastrosServico => Set<CadastroServico>();
    public DbSet<CadastroServicoParcela> CadastroServicoParcelas => Set<CadastroServicoParcela>();
    public DbSet<Prestador> Prestadores => Set<Prestador>();
    public DbSet<CadastroServicoPrestador> CadastroServicoPrestadores => Set<CadastroServicoPrestador>();
    public DbSet<CadastroServicoCodigoHistorico> CadastroServicoCodigoHistoricos => Set<CadastroServicoCodigoHistorico>();
    public DbSet<ServicoTipoCampoConfig> ServicoTipoCampoConfigs => Set<ServicoTipoCampoConfig>();
    public DbSet<AcompanhamentoServico> Acompanhamentos => Set<AcompanhamentoServico>();
    public DbSet<AcompanhamentoServicoHistorico> AcompanhamentoHistoricos => Set<AcompanhamentoServicoHistorico>();
    public DbSet<AcompanhamentoServicoSituacaoConfig> AcompanhamentoSituacoes => Set<AcompanhamentoServicoSituacaoConfig>();
    public DbSet<AcompanhamentoSituacaoPendenciaConfig> AcompanhamentoPendenciaConfigs => Set<AcompanhamentoSituacaoPendenciaConfig>();
    public DbSet<AcompanhamentoServicoPendencia> AcompanhamentoPendencias => Set<AcompanhamentoServicoPendencia>();
    public DbSet<CustoIndireto> CustosIndiretos => Set<CustoIndireto>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioPreferencia> UsuarioPreferencias => Set<UsuarioPreferencia>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();
    public DbSet<PdfTemplate> PdfTemplates => Set<PdfTemplate>();
    public DbSet<GoogleIntegration> GoogleIntegrations => Set<GoogleIntegration>();
    public DbSet<GoogleIntegrationAudit> GoogleIntegrationAudits => Set<GoogleIntegrationAudit>();
    public DbSet<GoogleSheetReportMetadata> GoogleSheetReports => Set<GoogleSheetReportMetadata>();
    public DbSet<WhatsAppMetaIntegration> WhatsAppIntegrations => Set<WhatsAppMetaIntegration>();
    public DbSet<WhatsAppMetaIntegrationAudit> WhatsAppIntegrationAudits => Set<WhatsAppMetaIntegrationAudit>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtlasDbContext).Assembly);
        modelBuilder.ApplySnakeCaseNames();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Um DbContext de Blazor Server pode permanecer vivo durante toda a sessão.
            // Não deixe entidades rejeitadas serem reenviadas nas próximas operações.
            ChangeTracker.Clear();
            throw;
        }
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                SetIfPresent(entry, nameof(CadastroServico.CreatedAt), now, onlyWhenDefault: true);
                SetIfPresent(entry, nameof(CadastroServico.UpdatedAt), now);
            }
            else if (entry.State == EntityState.Modified)
            {
                SetIfPresent(entry, nameof(CadastroServico.UpdatedAt), now);
            }
        }
    }

    private static void SetIfPresent(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        string propertyName,
        DateTime value,
        bool onlyWhenDefault = false)
    {
        var property = entry.Metadata.FindProperty(propertyName);
        if (property?.ClrType != typeof(DateTime))
        {
            return;
        }

        var trackedProperty = entry.Property(propertyName);
        if (!onlyWhenDefault || trackedProperty.CurrentValue is null || (DateTime)trackedProperty.CurrentValue == default)
        {
            trackedProperty.CurrentValue = value;
        }
    }
}

internal static class RelationalNamingExtensions
{
    public static void ApplySnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));

                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (clrType == typeof(DateTime))
                {
                    property.SetColumnType("timestamp with time zone");
                }
                else if (clrType == typeof(DateTimeOffset))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0 && value[index - 1] != '_')
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
