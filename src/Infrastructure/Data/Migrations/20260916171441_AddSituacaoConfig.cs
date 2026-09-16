using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSituacaoConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ativo",
                table: "orcamento_situacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "cor",
                table: "orcamento_situacoes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ordem",
                table: "orcamento_situacoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "padrao",
                table: "orcamento_situacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "configuracao_historico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contexto = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    escopo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    registro_id = table.Column<long>(type: "bigint", nullable: true),
                    registro_nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    acao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    detalhes = table.Column<string>(type: "text", nullable: true),
                    responsavel_nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracao_historico", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_configuracao_historico_contexto_data",
                table: "configuracao_historico",
                columns: new[] { "contexto", "created_at" });

            migrationBuilder.Sql("""
                UPDATE orcamento_situacoes SET ativo = true;
                """);

            migrationBuilder.Sql("""
                INSERT INTO orcamento_situacoes (label, closed, cor, ordem, padrao, ativo, created_at, updated_at)
                SELECT v.label, v.closed, v.cor, v.ordem, v.padrao, v.ativo, now(), now()
                FROM (VALUES
                    ('Em análise', false, '#2563EB', 0, true, true),
                    ('Aguardando cliente', false, '#D97706', 1, false, true),
                    ('Aprovado', true, '#16A34A', 2, false, true),
                    ('Recusado', true, '#DC2626', 3, false, true)
                ) AS v(label, closed, cor, ordem, padrao, ativo)
                WHERE NOT EXISTS (SELECT 1 FROM orcamento_situacoes);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracao_historico");

            migrationBuilder.DropColumn(
                name: "ativo",
                table: "orcamento_situacoes");

            migrationBuilder.DropColumn(
                name: "cor",
                table: "orcamento_situacoes");

            migrationBuilder.DropColumn(
                name: "ordem",
                table: "orcamento_situacoes");

            migrationBuilder.DropColumn(
                name: "padrao",
                table: "orcamento_situacoes");
        }
    }
}
