using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDashboardQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_lancamentos_dashboard",
                table: "lancamentos",
                columns: new[] { "data", "tipo", "cadastro_servico_id" });

            migrationBuilder.CreateIndex(
                name: "idx_custos_indiretos_dashboard",
                table: "custos_indiretos",
                columns: new[] { "data", "categoria" });

            migrationBuilder.CreateIndex(
                name: "idx_cadastro_servicos_dashboard",
                table: "cadastro_servicos",
                columns: new[] { "data_contrato", "tipo_servico" });

            migrationBuilder.CreateIndex(
                name: "idx_acompanhamento_dashboard_priority",
                table: "acompanhamento_servicos",
                columns: new[] { "tipo_servico", "ultima_mudanca_situacao_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_lancamentos_dashboard",
                table: "lancamentos");

            migrationBuilder.DropIndex(
                name: "idx_custos_indiretos_dashboard",
                table: "custos_indiretos");

            migrationBuilder.DropIndex(
                name: "idx_cadastro_servicos_dashboard",
                table: "cadastro_servicos");

            migrationBuilder.DropIndex(
                name: "idx_acompanhamento_dashboard_priority",
                table: "acompanhamento_servicos");
        }
    }
}
