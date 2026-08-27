using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260803195000_SeedAvcbOperationalStatuses")]
public sealed class SeedAvcbOperationalStatuses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO acompanhamento_servico_situacao_config
                (tipo_servico, nome, ordem, situacao_inicial, ativo)
            VALUES
                ('AVCB', 'Aguar. Contratante', 10, true, true),
                ('AVCB', 'Aguar. Documentos', 20, false, true),
                ('AVCB', 'Comunicado', 30, false, true),
                ('AVCB', 'Concluído', 40, false, true),
                ('AVCB', 'Concluído - Aguar. Pag.', 50, false, true),
                ('AVCB', 'Em análise', 60, false, true),
                ('AVCB', 'Executar', 70, false, true)
            ON CONFLICT (tipo_servico, nome) DO UPDATE SET
                ordem = EXCLUDED.ordem,
                ativo = true;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Não apagar configurações que possam estar vinculadas a históricos.
    }
}
