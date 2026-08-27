using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260803183000_SplitCadastroServicoCompanyAddress")]
public partial class SplitCadastroServicoCompanyAddress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "endereco_empresa_rua", table: "cadastro_servicos", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "endereco_empresa_numero", table: "cadastro_servicos", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "endereco_empresa_bairro", table: "cadastro_servicos", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "endereco_empresa_complemento", table: "cadastro_servicos", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "endereco_empresa_cidade", table: "cadastro_servicos", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "endereco_empresa_estado", table: "cadastro_servicos", type: "character varying(2)", maxLength: 2, nullable: true);
        migrationBuilder.AddColumn<string>(name: "endereco_empresa_cep", table: "cadastro_servicos", type: "text", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "endereco_empresa_rua", table: "cadastro_servicos");
        migrationBuilder.DropColumn(name: "endereco_empresa_numero", table: "cadastro_servicos");
        migrationBuilder.DropColumn(name: "endereco_empresa_bairro", table: "cadastro_servicos");
        migrationBuilder.DropColumn(name: "endereco_empresa_complemento", table: "cadastro_servicos");
        migrationBuilder.DropColumn(name: "endereco_empresa_cidade", table: "cadastro_servicos");
        migrationBuilder.DropColumn(name: "endereco_empresa_estado", table: "cadastro_servicos");
        migrationBuilder.DropColumn(name: "endereco_empresa_cep", table: "cadastro_servicos");
    }
}
