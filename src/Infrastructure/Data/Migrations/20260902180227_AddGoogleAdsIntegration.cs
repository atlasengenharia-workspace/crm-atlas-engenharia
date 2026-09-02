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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleAdsCampaignMetrics");

            migrationBuilder.DropTable(
                name: "GoogleAdsIntegrationAudits");

            migrationBuilder.DropTable(
                name: "GoogleAdsLeads");

            migrationBuilder.DropTable(
                name: "GoogleAdsCampaigns");

            migrationBuilder.DropTable(
                name: "GoogleAdsIntegrations");
        }
    }
}
