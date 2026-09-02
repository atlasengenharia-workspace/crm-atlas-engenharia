using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAdsIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "convertido_em",
                table: "orcamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "servico_convertido_codigo",
                table: "orcamentos",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "servico_convertido_id",
                table: "orcamentos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subtipo",
                table: "orcamentos",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "entrada",
                table: "condicoes_pagamento",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tipo_valor_parcela",
                table: "condicoes_pagamento",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "valor_parcela",
                table: "condicoes_pagamento",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_servico_bairro",
                table: "cadastro_servicos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_servico_cep",
                table: "cadastro_servicos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_servico_cidade",
                table: "cadastro_servicos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_servico_complemento",
                table: "cadastro_servicos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_servico_estado",
                table: "cadastro_servicos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_servico_numero",
                table: "cadastro_servicos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "endereco_servico_rua",
                table: "cadastro_servicos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "observacao",
                table: "cadastro_servicos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "valor_nota_fiscal_dividido",
                table: "cadastro_servicos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "valor_nota_fiscal_parcela",
                table: "cadastro_servicos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cadastro_servico_codigo_historico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    servico_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_anterior = table.Column<string>(type: "text", nullable: true),
                    codigo_novo = table.Column<string>(type: "text", nullable: true),
                    responsavel = table.Column<string>(type: "text", nullable: true),
                    alterado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cadastro_servico_codigo_historico", x => x.id);
                    table.ForeignKey(
                        name: "FK_cadastro_servico_codigo_historico_cadastro_servicos_servico~",
                        column: x => x.servico_id,
                        principalTable: "cadastro_servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoogleAdsIntegrations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    developer_token = table.Column<string>(type: "text", nullable: true),
                    client_id = table.Column<string>(type: "text", nullable: true),
                    encrypted_client_secret = table.Column<string>(type: "text", nullable: true),
                    encrypted_refresh_token = table.Column<string>(type: "text", nullable: true),
                    login_customer_id = table.Column<string>(type: "text", nullable: true),
                    auto_sync = table.Column<bool>(type: "boolean", nullable: false),
                    sync_interval_min = table.Column<int>(type: "integer", nullable: false),
                    last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    import_leads_as_budgets = table.Column<bool>(type: "boolean", nullable: false),
                    create_financial_entries = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsIntegrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_historico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    valor_anterior = table.Column<string>(type: "text", nullable: true),
                    valor_novo = table.Column<string>(type: "text", nullable: true),
                    responsavel = table.Column<string>(type: "text", nullable: true),
                    alterado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_historico", x => x.id);
                    table.ForeignKey(
                        name: "FK_orcamento_historico_orcamentos_orcamento_id",
                        column: x => x.orcamento_id,
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "servico_tipo_campo_config",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_servico = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    campo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    visivel = table.Column<bool>(type: "boolean", nullable: false),
                    obrigatorio = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servico_tipo_campo_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "GoogleAdsCampaigns",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_id = table.Column<long>(type: "bigint", nullable: false),
                    external_campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    budget_amount_micros = table.Column<long>(type: "bigint", nullable: true),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsCampaigns", x => x.id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsCampaigns_GoogleAdsIntegrations_integration_id",
                        column: x => x.integration_id,
                        principalTable: "GoogleAdsIntegrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoogleAdsIntegrationAudits",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_id = table.Column<long>(type: "bigint", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    result_status = table.Column<int>(type: "integer", nullable: false),
                    actor = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsIntegrationAudits", x => x.id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsIntegrationAudits_GoogleAdsIntegrations_integratio~",
                        column: x => x.integration_id,
                        principalTable: "GoogleAdsIntegrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoogleAdsCampaignMetrics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_id = table.Column<long>(type: "bigint", nullable: false),
                    campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    cost_micros = table.Column<long>(type: "bigint", nullable: false),
                    clicks = table.Column<long>(type: "bigint", nullable: false),
                    impressions = table.Column<long>(type: "bigint", nullable: false),
                    conversions = table.Column<double>(type: "double precision", nullable: false),
                    conversions_value = table.Column<double>(type: "double precision", nullable: false),
                    all_conversions = table.Column<double>(type: "double precision", nullable: false),
                    ctr = table.Column<double>(type: "double precision", nullable: false),
                    cpm = table.Column<double>(type: "double precision", nullable: false),
                    cpc = table.Column<double>(type: "double precision", nullable: false),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsCampaignMetrics", x => x.id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsCampaignMetrics_GoogleAdsCampaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "GoogleAdsCampaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoogleAdsCampaignMetrics_GoogleAdsIntegrations_integration_~",
                        column: x => x.integration_id,
                        principalTable: "GoogleAdsIntegrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoogleAdsLeads",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_id = table.Column<long>(type: "bigint", nullable: false),
                    campaign_id = table.Column<long>(type: "bigint", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    gcl_id = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    converted_to_budget = table.Column<bool>(type: "boolean", nullable: false),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: true),
                    converted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAdsLeads", x => x.id);
                    table.ForeignKey(
                        name: "FK_GoogleAdsLeads_GoogleAdsCampaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "GoogleAdsCampaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoogleAdsLeads_GoogleAdsIntegrations_integration_id",
                        column: x => x.integration_id,
                        principalTable: "GoogleAdsIntegrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_orcamentos_servico",
                table: "orcamentos",
                column: "servico_convertido_id");

            migrationBuilder.CreateIndex(
                name: "IX_cadastro_servico_codigo_historico_servico_id",
                table: "cadastro_servico_codigo_historico",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsCampaignMetrics_campaign_id",
                table: "GoogleAdsCampaignMetrics",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsCampaignMetrics_integration_id",
                table: "GoogleAdsCampaignMetrics",
                column: "integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsCampaigns_integration_id",
                table: "GoogleAdsCampaigns",
                column: "integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsIntegrationAudits_integration_id",
                table: "GoogleAdsIntegrationAudits",
                column: "integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsLeads_campaign_id",
                table: "GoogleAdsLeads",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAdsLeads_integration_id",
                table: "GoogleAdsLeads",
                column: "integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_historico_orcamento_id",
                table: "orcamento_historico",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_servico_tipo_campo_config_tipo_servico_campo",
                table: "servico_tipo_campo_config",
                columns: new[] { "tipo_servico", "campo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cadastro_servico_codigo_historico");

            migrationBuilder.DropTable(
                name: "GoogleAdsCampaignMetrics");

            migrationBuilder.DropTable(
                name: "GoogleAdsIntegrationAudits");

            migrationBuilder.DropTable(
                name: "GoogleAdsLeads");

            migrationBuilder.DropTable(
                name: "orcamento_historico");

            migrationBuilder.DropTable(
                name: "servico_tipo_campo_config");

            migrationBuilder.DropTable(
                name: "GoogleAdsCampaigns");

            migrationBuilder.DropTable(
                name: "GoogleAdsIntegrations");

            migrationBuilder.DropIndex(
                name: "idx_orcamentos_servico",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "convertido_em",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "servico_convertido_codigo",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "servico_convertido_id",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "subtipo",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "entrada",
                table: "condicoes_pagamento");

            migrationBuilder.DropColumn(
                name: "tipo_valor_parcela",
                table: "condicoes_pagamento");

            migrationBuilder.DropColumn(
                name: "valor_parcela",
                table: "condicoes_pagamento");

            migrationBuilder.DropColumn(
                name: "endereco_servico_bairro",
                table: "cadastro_servicos");

            migrationBuilder.DropColumn(
                name: "endereco_servico_cep",
                table: "cadastro_servicos");

            migrationBuilder.DropColumn(
                name: "endereco_servico_cidade",
                table: "cadastro_servicos");

            migrationBuilder.DropColumn(
                name: "endereco_servico_complemento",
                table: "cadastro_servicos");

            migrationBuilder.DropColumn(
                name: "endereco_servico_estado",
                table: "cadastro_servicos");

            migrationBuilder.DropColumn(
                name: "endereco_servico_numero",
                table: "cadastro_servicos");

            migrationBuilder.DropColumn(
                name: "endereco_servico_rua",
                table: "cadastro_servicos");

            migrationBuilder.DropColumn(
                name: "observacao",
                table: "cadastro_servicos");

            migrationBuilder.DropColumn(
                name: "valor_nota_fiscal_dividido",
                table: "cadastro_servicos");

            migrationBuilder.DropColumn(
                name: "valor_nota_fiscal_parcela",
                table: "cadastro_servicos");
        }
    }
}
