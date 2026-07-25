using CrmAtlas.ApplicationCore.Acompanhamentos;
using CrmAtlas.ApplicationCore.Clientes;
using CrmAtlas.ApplicationCore.Documentos;
using CrmAtlas.ApplicationCore.Financeiro;
using CrmAtlas.ApplicationCore.Identidade;
using CrmAtlas.ApplicationCore.Integracoes;
using CrmAtlas.ApplicationCore.Notificacoes;
using CrmAtlas.ApplicationCore.Servicos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmAtlas.Infrastructure.Data;

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.HasIndex(x => x.CnpjCpf).IsUnique();
        builder.Property(x => x.CnpjCpf).HasMaxLength(18).IsRequired();
        builder.Property(x => x.RazaoSocial).IsRequired();
    }
}

internal sealed class ServicosConfiguration :
    IEntityTypeConfiguration<Avcb>,
    IEntityTypeConfiguration<Clcb>,
    IEntityTypeConfiguration<Obra>,
    IEntityTypeConfiguration<ProcessoAdm>,
    IEntityTypeConfiguration<CondicaoPagamento>,
    IEntityTypeConfiguration<Orcamento>,
    IEntityTypeConfiguration<OrcamentoSituacao>,
    IEntityTypeConfiguration<CadastroServico>,
    IEntityTypeConfiguration<CadastroServicoParcela>,
    IEntityTypeConfiguration<Prestador>,
    IEntityTypeConfiguration<CadastroServicoPrestador>
{
    public void Configure(EntityTypeBuilder<Avcb> builder)
    {
        builder.ToTable("avcbs");
        ConfigureUniqueCode(builder);
        builder.Property(x => x.Situacao).HasConversion<string>();
    }

    public void Configure(EntityTypeBuilder<Clcb> builder)
    {
        builder.ToTable("clcbs");
        ConfigureUniqueCode(builder);
        builder.Property(x => x.Situacao).HasConversion<string>();
    }

    public void Configure(EntityTypeBuilder<Obra> builder)
    {
        builder.ToTable("obras");
        ConfigureUniqueCode(builder);
        builder.Property(x => x.Situacao).HasConversion<string>();
    }

    public void Configure(EntityTypeBuilder<ProcessoAdm> builder)
    {
        builder.ToTable("processos_adm");
        builder.Property(x => x.Situacao).HasConversion<string>();
    }

    public void Configure(EntityTypeBuilder<CondicaoPagamento> builder)
    {
        builder.ToTable("condicoes_pagamento");
        builder.HasIndex(x => x.Nome).IsUnique();
        builder.Property(x => x.Nome).IsRequired();
    }

    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("orcamentos");
        ConfigureUniqueCode(builder);
        builder.Property(x => x.Descricao).HasMaxLength(2000);
        builder.Property(x => x.Situacao).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TipoServico).HasConversion<string>().HasMaxLength(32);
    }

    public void Configure(EntityTypeBuilder<OrcamentoSituacao> builder)
    {
        builder.ToTable("orcamento_situacoes");
        builder.HasIndex(x => x.Label).IsUnique();
        builder.Property(x => x.Label).HasMaxLength(80).IsRequired();
    }

    public void Configure(EntityTypeBuilder<CadastroServico> builder)
    {
        builder.ToTable("cadastro_servicos");
        ConfigureUniqueCode(builder);
        builder.HasIndex(x => new { x.DataContrato, x.TipoServico })
            .HasDatabaseName("idx_cadastro_servicos_dashboard");
        builder.Property(x => x.TipoServico).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.EnderecoEmpresa).HasColumnType("text");
        builder.Property(x => x.EnderecoServico).HasColumnType("text");
        builder.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId);
        builder.HasOne(x => x.Orcamento).WithMany().HasForeignKey(x => x.OrcamentoId);
        builder.HasOne(x => x.CondicaoPagamento).WithMany().HasForeignKey(x => x.CondicaoPagamentoId);
        builder.HasMany(x => x.Parcelas).WithOne(x => x.CadastroServico)
            .HasForeignKey(x => x.CadastroServicoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Prestadores).WithOne(x => x.CadastroServico)
            .HasForeignKey(x => x.CadastroServicoId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<CadastroServicoParcela> builder) =>
        builder.ToTable("cadastro_servico_parcelas");

    public void Configure(EntityTypeBuilder<Prestador> builder) =>
        builder.ToTable("prestadores");

    public void Configure(EntityTypeBuilder<CadastroServicoPrestador> builder)
    {
        builder.ToTable("cadastro_servico_prestadores");
        builder.Property(x => x.DataPagamentoTipo).HasConversion<string>().HasMaxLength(24);
        builder.HasOne(x => x.Prestador).WithMany().HasForeignKey(x => x.PrestadorId);
    }

    private static void ConfigureUniqueCode<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.HasIndex("Codigo").IsUnique();
        builder.Property("Codigo").IsRequired();
    }
}

internal sealed class AcompanhamentoConfiguration :
    IEntityTypeConfiguration<AcompanhamentoServico>,
    IEntityTypeConfiguration<AcompanhamentoServicoHistorico>,
    IEntityTypeConfiguration<AcompanhamentoServicoSituacaoConfig>,
    IEntityTypeConfiguration<AcompanhamentoSituacaoPendenciaConfig>,
    IEntityTypeConfiguration<AcompanhamentoServicoPendencia>
{
    public void Configure(EntityTypeBuilder<AcompanhamentoServico> builder)
    {
        builder.ToTable("acompanhamento_servicos");
        builder.HasIndex(x => new { x.TipoServico, x.OrigemId })
            .IsUnique().HasDatabaseName("uk_acompanhamento_servico_origem");
        builder.HasIndex(x => x.Codigo)
            .IsUnique().HasDatabaseName("uk_acompanhamento_servico_codigo");
        builder.HasIndex(x => new { x.TipoServico, x.UltimaMudancaSituacaoEm })
            .HasDatabaseName("idx_acompanhamento_dashboard_priority");
        builder.Property(x => x.TipoServico).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Codigo).IsRequired();
        builder.Property(x => x.CnpjCpf).HasMaxLength(32);
        builder.Property(x => x.Situacao).IsRequired();
        builder.Property(x => x.Descricao).HasColumnType("text");
        builder.Property(x => x.UltimaMudancaSituacaoEm).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.HasMany(x => x.Historicos).WithOne(x => x.Servico)
            .HasForeignKey(x => x.ServicoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Pendencias).WithOne(x => x.Servico)
            .HasForeignKey(x => x.ServicoId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<AcompanhamentoServicoHistorico> builder)
    {
        builder.ToTable("acompanhamento_servico_historico");
        builder.Property(x => x.NovaSituacao).IsRequired();
        builder.Property(x => x.Descricao).HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
    }

    public void Configure(EntityTypeBuilder<AcompanhamentoServicoSituacaoConfig> builder)
    {
        builder.ToTable("acompanhamento_servico_situacao_config");
        builder.HasIndex(x => new { x.TipoServico, x.Nome }).IsUnique()
            .HasDatabaseName("uk_acompanhamento_situacao_tipo_nome");
        builder.Property(x => x.TipoServico).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Nome).IsRequired();
        builder.HasMany(x => x.Pendencias).WithOne(x => x.SituacaoConfig)
            .HasForeignKey(x => x.SituacaoConfigId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<AcompanhamentoSituacaoPendenciaConfig> builder)
    {
        builder.ToTable("acompanhamento_situacao_pendencias");
        builder.HasIndex(x => new { x.SituacaoConfigId, x.Label }).IsUnique()
            .HasDatabaseName("uk_acompanhamento_pendencia_situacao_label");
        builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
    }

    public void Configure(EntityTypeBuilder<AcompanhamentoServicoPendencia> builder)
    {
        builder.ToTable("acompanhamento_servico_pendencias");
        builder.HasIndex(x => x.ServicoId).HasDatabaseName("idx_acompanhamento_pendencia_servico");
        builder.HasIndex(x => x.SituacaoConfigId).HasDatabaseName("idx_acompanhamento_pendencia_situacao");
        builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ConcluidaEm).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.HasOne(x => x.SituacaoConfig).WithMany().HasForeignKey(x => x.SituacaoConfigId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.PendenciaConfig).WithMany().HasForeignKey(x => x.PendenciaConfigId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class FinanceiroConfiguration :
    IEntityTypeConfiguration<CustoIndireto>,
    IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<CustoIndireto> builder)
    {
        builder.ToTable("custos_indiretos");
        builder.HasIndex(x => new { x.Data, x.Categoria })
            .HasDatabaseName("idx_custos_indiretos_dashboard");
        builder.Property(x => x.Data).IsRequired();
        builder.Property(x => x.Descricao).IsRequired();
        builder.Property(x => x.Valor).IsRequired();
        builder.Property(x => x.Categoria).IsRequired();
    }

    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");
        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.HasIndex(x => new { x.Data, x.Tipo, x.CadastroServicoId })
            .HasDatabaseName("idx_lancamentos_dashboard");
        builder.Property(x => x.Codigo).IsRequired();
        builder.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Origem).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Observacao).HasColumnType("text");
        builder.HasOne(x => x.CadastroServico).WithMany().HasForeignKey(x => x.CadastroServicoId);
        builder.HasOne(x => x.Prestador).WithMany().HasForeignKey(x => x.PrestadorId);
    }
}

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Username).IsUnique();
        builder.HasIndex(x => x.Auth0Sub).IsUnique();
        builder.Property(x => x.NomeCompleto).IsRequired();
        builder.Property(x => x.Email).IsRequired();
        builder.Property(x => x.Username).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password");
        builder.Property(x => x.Role).HasConversion<string>();
    }
}

internal sealed class UsuarioPreferenciaConfiguration : IEntityTypeConfiguration<UsuarioPreferencia>
{
    public void Configure(EntityTypeBuilder<UsuarioPreferencia> builder)
    {
        builder.ToTable("usuario_preferencias");
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.Theme).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TableDensity).HasMaxLength(20).IsRequired();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class NotificationConfiguration :
    IEntityTypeConfiguration<Notification>,
    IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ServiceType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.RuleType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Amount).HasPrecision(38, 2);
        builder.Property(x => x.ReferenceKey).HasMaxLength(255);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<NotificationRule> builder)
    {
        builder.ToTable("notification_rules");
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
    }
}

internal sealed class PdfTemplateConfiguration : IEntityTypeConfiguration<PdfTemplate>
{
    public void Configure(EntityTypeBuilder<PdfTemplate> builder)
    {
        builder.ToTable("pdf_templates");
        builder.HasIndex(x => x.Key).IsUnique().HasDatabaseName("uk_pdf_template_key");
        builder.Property(x => x.Key).HasColumnName("template_key").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Html).HasColumnType("text").IsRequired();
    }
}

internal sealed class IntegrationConfiguration :
    IEntityTypeConfiguration<GoogleIntegration>,
    IEntityTypeConfiguration<GoogleIntegrationScope>,
    IEntityTypeConfiguration<GoogleIntegrationAudit>,
    IEntityTypeConfiguration<GoogleSheetReportMetadata>,
    IEntityTypeConfiguration<WhatsAppMetaIntegration>,
    IEntityTypeConfiguration<WhatsAppMetaIntegrationAudit>
{
    public void Configure(EntityTypeBuilder<GoogleIntegration> builder)
    {
        builder.ToTable("google_integrations");
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Key).HasColumnName("integration_key").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AccountEmail).HasMaxLength(255);
        builder.Property(x => x.ClientId).HasMaxLength(255);
        builder.Property(x => x.EncryptedClientSecret).HasColumnName("client_secret").HasColumnType("text");
        builder.Property(x => x.RedirectUri).HasMaxLength(500);
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);
        builder.HasMany(x => x.Scopes).WithOne(x => x.Integration)
            .HasForeignKey(x => x.IntegrationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Audits).WithOne(x => x.Integration)
            .HasForeignKey(x => x.IntegrationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Reports).WithOne(x => x.Integration)
            .HasForeignKey(x => x.IntegrationId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<GoogleIntegrationScope> builder)
    {
        builder.ToTable("google_integration_scopes");
        builder.HasKey(x => new { x.IntegrationId, x.Scope });
        builder.Property(x => x.Scope).HasMaxLength(255);
    }

    public void Configure(EntityTypeBuilder<GoogleIntegrationAudit> builder)
    {
        builder.ToTable("google_integration_audit");
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ResultStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Actor).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(500);
    }

    public void Configure(EntityTypeBuilder<GoogleSheetReportMetadata> builder)
    {
        builder.ToTable("google_sheet_report_metadata");
        builder.HasIndex(x => new { x.IntegrationId, x.ReportName }).IsUnique()
            .HasDatabaseName("uk_google_sheet_report_name_integration");
        builder.Property(x => x.ReportName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SpreadsheetId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SpreadsheetUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedRange).HasMaxLength(120);
    }

    public void Configure(EntityTypeBuilder<WhatsAppMetaIntegration> builder)
    {
        builder.ToTable("whatsapp_meta_integrations");
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Key).HasColumnName("integration_key").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.PhoneNumberId).HasMaxLength(120);
        builder.Property(x => x.BusinessAccountId).HasMaxLength(120);
        builder.Property(x => x.EncryptedPermanentToken).HasColumnName("permanent_token").HasColumnType("text");
        builder.Property(x => x.EncryptedWebhookVerifyToken).HasColumnName("webhook_verify_token").HasColumnType("text");
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);
        builder.HasMany(x => x.Audits).WithOne(x => x.Integration)
            .HasForeignKey(x => x.IntegrationId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<WhatsAppMetaIntegrationAudit> builder)
    {
        builder.ToTable("whatsapp_meta_integration_audit");
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ResultStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Actor).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(500);
    }
}
