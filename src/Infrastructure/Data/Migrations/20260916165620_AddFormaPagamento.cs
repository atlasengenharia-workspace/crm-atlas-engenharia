using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFormaPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "formas_pagamento",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formas_pagamento", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_formas_pagamento_nome",
                table: "formas_pagamento",
                column: "nome",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO formas_pagamento (nome, created_at, updated_at)
                SELECT v.nome, now(), now()
                FROM (VALUES ('PIX'), ('Boleto'), ('Transferência'), ('Cartão')) AS v(nome)
                WHERE NOT EXISTS (SELECT 1 FROM formas_pagamento);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "formas_pagamento");
        }
    }
}
