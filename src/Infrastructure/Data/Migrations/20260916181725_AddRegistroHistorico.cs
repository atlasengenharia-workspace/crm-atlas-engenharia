using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistroHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registro_historico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entidade_id = table.Column<long>(type: "bigint", nullable: false),
                    entidade_codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    campo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    valor_anterior = table.Column<string>(type: "text", nullable: true),
                    valor_novo = table.Column<string>(type: "text", nullable: true),
                    responsavel_nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registro_historico", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_registro_historico_entidade",
                table: "registro_historico",
                columns: new[] { "entidade", "entidade_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registro_historico");
        }
    }
}
