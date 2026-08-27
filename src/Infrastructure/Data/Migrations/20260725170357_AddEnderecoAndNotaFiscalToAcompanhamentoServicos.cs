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
            migrationBuilder.Sql("""
                ALTER TABLE acompanhamento_servicos ADD COLUMN IF NOT EXISTS cnpj_cpf text;
                ALTER TABLE acompanhamento_servicos ADD COLUMN IF NOT EXISTS endereco text;
                ALTER TABLE acompanhamento_servicos ADD COLUMN IF NOT EXISTS nota_fiscal text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cnpj_cpf",
                table: "acompanhamento_servicos");

            migrationBuilder.DropColumn(
                name: "endereco",
                table: "acompanhamento_servicos");

            migrationBuilder.DropColumn(
                name: "nota_fiscal",
                table: "acompanhamento_servicos");
        }
    }
}
