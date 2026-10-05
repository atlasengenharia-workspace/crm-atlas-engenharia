using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetNullOnDeleteClienteCadastroServico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cadastro_servicos_clientes_cliente_id",
                table: "cadastro_servicos");

            migrationBuilder.DropForeignKey(
                name: "FK_cadastro_servicos_condicoes_pagamento_condicao_pagamento_id",
                table: "cadastro_servicos");

            migrationBuilder.DropForeignKey(
                name: "FK_cadastro_servicos_orcamentos_orcamento_id",
                table: "cadastro_servicos");

            migrationBuilder.AddForeignKey(
                name: "FK_cadastro_servicos_clientes_cliente_id",
                table: "cadastro_servicos",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_cadastro_servicos_condicoes_pagamento_condicao_pagamento_id",
                table: "cadastro_servicos",
                column: "condicao_pagamento_id",
                principalTable: "condicoes_pagamento",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_cadastro_servicos_orcamentos_orcamento_id",
                table: "cadastro_servicos",
                column: "orcamento_id",
                principalTable: "orcamentos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cadastro_servicos_clientes_cliente_id",
                table: "cadastro_servicos");

            migrationBuilder.DropForeignKey(
                name: "FK_cadastro_servicos_condicoes_pagamento_condicao_pagamento_id",
                table: "cadastro_servicos");

            migrationBuilder.DropForeignKey(
                name: "FK_cadastro_servicos_orcamentos_orcamento_id",
                table: "cadastro_servicos");

            migrationBuilder.AddForeignKey(
                name: "FK_cadastro_servicos_clientes_cliente_id",
                table: "cadastro_servicos",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_cadastro_servicos_condicoes_pagamento_condicao_pagamento_id",
                table: "cadastro_servicos",
                column: "condicao_pagamento_id",
                principalTable: "condicoes_pagamento",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_cadastro_servicos_orcamentos_orcamento_id",
                table: "cadastro_servicos",
                column: "orcamento_id",
                principalTable: "orcamentos",
                principalColumn: "id");
        }
    }
}
