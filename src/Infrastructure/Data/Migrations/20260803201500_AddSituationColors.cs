using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260803201500_AddSituationColors")]
public sealed class AddSituationColors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE acompanhamento_servico_situacao_config
                ADD COLUMN IF NOT EXISTS cor character varying(16);

            UPDATE acompanhamento_servico_situacao_config SET cor = CASE
                WHEN lower(nome) LIKE 'agendado%' THEN '#C7D2FE'
                WHEN lower(nome) LIKE 'aguar%contratante%' OR lower(nome) LIKE 'aguard%cliente%' THEN '#FDE68A'
                WHEN lower(nome) LIKE 'aguar%document%' THEN '#FDBA74'
                WHEN lower(nome) LIKE 'comunicado%' THEN '#FCA5A5'
                WHEN lower(nome) LIKE 'concluído%aguar%pag%' OR lower(nome) LIKE 'concluido%aguar%pag%' THEN '#93C5FD'
                WHEN lower(nome) LIKE 'concluído%' OR lower(nome) LIKE 'concluido%' THEN '#86EFAC'
                WHEN lower(nome) LIKE 'em análise%' OR lower(nome) LIKE 'em analise%' THEN '#D1D5DB'
                WHEN lower(nome) LIKE 'executar%' THEN '#C4B5FD'
                WHEN lower(nome) LIKE '%vistoria%' THEN '#67E8F9'
                ELSE '#BFDBFE'
            END
            WHERE cor IS NULL OR btrim(cor) = '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE acompanhamento_servico_situacao_config DROP COLUMN IF EXISTS cor;");
    }
}
