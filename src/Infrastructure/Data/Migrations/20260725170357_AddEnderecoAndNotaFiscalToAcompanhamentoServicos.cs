using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnderecoAndNotaFiscalToAcompanhamentoServicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "endereco",
                table: "acompanhamento_servicos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nota_fiscal",
                table: "acompanhamento_servicos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "endereco",
                table: "acompanhamento_servicos");

            migrationBuilder.DropColumn(
                name: "nota_fiscal",
                table: "acompanhamento_servicos");
        }
    }
}
