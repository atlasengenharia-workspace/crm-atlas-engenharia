using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations;

/// <summary>
/// A coluna "Prox. Parcela" da planilha nem sempre traz data: na aba de
/// Processos Adm ela é preenchida com a próxima ação ("Finalizar",
/// "Protocolar"). Esse texto não cabia em <c>proxima_parcela</c> (date) e era
/// descartado na importação, deixando a coluna vazia no CRM.
/// </summary>
public partial class AddProximaParcelaTextoToAcompanhamentos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE acompanhamento_servicos
                ADD COLUMN IF NOT EXISTS proxima_parcela_texto text;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "proxima_parcela_texto",
            table: "acompanhamento_servicos");
    }
}
