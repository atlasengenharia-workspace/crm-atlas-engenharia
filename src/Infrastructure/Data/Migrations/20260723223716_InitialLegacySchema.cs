using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CrmAtlas.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialLegacySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "acompanhamento_servico_situacao_config",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_servico = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: true),
                    situacao_inicial = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acompanhamento_servico_situacao_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "acompanhamento_servicos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_servico = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    origem_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nome_cliente = table.Column<string>(type: "text", nullable: true),
                    endereco = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    subtipo = table.Column<string>(type: "text", nullable: true),
                    situacao = table.Column<string>(type: "text", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    valor_contrato = table.Column<decimal>(type: "numeric", nullable: true),
                    data_contrato = table.Column<DateOnly>(type: "date", nullable: true),
                    nota_fiscal = table.Column<string>(type: "text", nullable: true),
                    condicao_pagamento = table.Column<string>(type: "text", nullable: true),
                    a_receber = table.Column<decimal>(type: "numeric", nullable: true),
                    recebido = table.Column<decimal>(type: "numeric", nullable: true),
                    custos = table.Column<decimal>(type: "numeric", nullable: true),
                    folder_url = table.Column<string>(type: "text", nullable: true),
                    ultima_mudanca_situacao_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acompanhamento_servicos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "avcbs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nome_cliente = table.Column<string>(type: "text", nullable: true),
                    endereco = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    servico = table.Column<string>(type: "text", nullable: true),
                    situacao = table.Column<string>(type: "text", nullable: true),
                    descricao_situacao = table.Column<string>(type: "text", nullable: true),
                    valor_contrato = table.Column<decimal>(type: "numeric", nullable: true),
                    data_contrato = table.Column<DateOnly>(type: "date", nullable: true),
                    nf = table.Column<string>(type: "text", nullable: true),
                    condicao_pagamento = table.Column<string>(type: "text", nullable: true),
                    a_receber = table.Column<decimal>(type: "numeric", nullable: true),
                    recebido = table.Column<decimal>(type: "numeric", nullable: true),
                    custos = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avcbs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clcbs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nome_cliente = table.Column<string>(type: "text", nullable: true),
                    endereco = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    situacao = table.Column<string>(type: "text", nullable: true),
                    descricao_situacao = table.Column<string>(type: "text", nullable: true),
                    valor_contrato = table.Column<decimal>(type: "numeric", nullable: true),
                    nf = table.Column<string>(type: "text", nullable: true),
                    data_contrato = table.Column<DateOnly>(type: "date", nullable: true),
                    a_receber = table.Column<decimal>(type: "numeric", nullable: true),
                    recebido = table.Column<decimal>(type: "numeric", nullable: true),
                    custos = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clcbs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cnpj_cpf = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    razao_social = table.Column<string>(type: "text", nullable: false),
                    nome_contato = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    rua = table.Column<string>(type: "text", nullable: true),
                    numero = table.Column<string>(type: "text", nullable: true),
                    bairro = table.Column<string>(type: "text", nullable: true),
                    complemento = table.Column<string>(type: "text", nullable: true),
                    cidade = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: true),
                    cep = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "condicoes_pagamento",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "text", nullable: false),
                    quantidade_parcelas = table.Column<int>(type: "integer", nullable: true),
                    intervalo_dias = table.Column<int>(type: "integer", nullable: true),
                    indefinido = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_condicoes_pagamento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custos_indiretos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    valor = table.Column<decimal>(type: "numeric", nullable: false),
                    categoria = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custos_indiretos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "google_integrations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    client_secret = table.Column<string>(type: "text", nullable: true),
                    redirect_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    webhook_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    auto_sync = table.Column<bool>(type: "boolean", nullable: false),
                    sync_interval_min = table.Column<int>(type: "integer", nullable: false),
                    last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_integrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_rules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    days_threshold = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "obras",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nome_cliente = table.Column<string>(type: "text", nullable: true),
                    endereco = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    servico = table.Column<string>(type: "text", nullable: true),
                    situacao = table.Column<string>(type: "text", nullable: true),
                    descricao_situacao = table.Column<string>(type: "text", nullable: true),
                    valor_contrato = table.Column<decimal>(type: "numeric", nullable: true),
                    data_contrato = table.Column<DateOnly>(type: "date", nullable: true),
                    nf = table.Column<string>(type: "text", nullable: true),
                    condicao_pagamento = table.Column<string>(type: "text", nullable: true),
                    a_receber = table.Column<decimal>(type: "numeric", nullable: true),
                    recebido = table.Column<decimal>(type: "numeric", nullable: true),
                    custos = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obras", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orcamento_situacoes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    closed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento_situacoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orcamentos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: true),
                    descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    situacao = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    tipo_servico = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamentos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pdf_templates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    html = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdf_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prestadores",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "text", nullable: true),
                    cnpj_cpf = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    metodo_pagamento = table.Column<string>(type: "text", nullable: true),
                    chave_pix = table.Column<string>(type: "text", nullable: true),
                    banco = table.Column<string>(type: "text", nullable: true),
                    agencia = table.Column<string>(type: "text", nullable: true),
                    conta = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prestadores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processos_adm",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    situacao = table.Column<string>(type: "text", nullable: true),
                    descricao_situacao = table.Column<string>(type: "text", nullable: true),
                    nome_cliente = table.Column<string>(type: "text", nullable: true),
                    codigo = table.Column<string>(type: "text", nullable: true),
                    servico = table.Column<string>(type: "text", nullable: true),
                    valor_contrato = table.Column<decimal>(type: "numeric", nullable: true),
                    data_contrato = table.Column<DateOnly>(type: "date", nullable: true),
                    nf = table.Column<string>(type: "text", nullable: true),
                    condicao_pagamento = table.Column<string>(type: "text", nullable: true),
                    proxima_parcela = table.Column<DateOnly>(type: "date", nullable: true),
                    a_receber = table.Column<decimal>(type: "numeric", nullable: true),
                    recebido = table.Column<decimal>(type: "numeric", nullable: true),
                    custos = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processos_adm", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome_completo = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    auth0_sub = table.Column<string>(type: "text", nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    verification_code = table.Column<string>(type: "text", nullable: true),
                    profile_picture_url = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_meta_integrations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    phone_number_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    business_account_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    permanent_token = table.Column<string>(type: "text", nullable: true),
                    webhook_verify_token = table.Column<string>(type: "text", nullable: true),
                    webhook_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_meta_integrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "acompanhamento_situacao_pendencias",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    situacao_config_id = table.Column<long>(type: "bigint", nullable: false),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acompanhamento_situacao_pendencias", x => x.id);
                    table.ForeignKey(
                        name: "FK_acompanhamento_situacao_pendencias_acompanhamento_servico_s~",
                        column: x => x.situacao_config_id,
                        principalTable: "acompanhamento_servico_situacao_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "acompanhamento_servico_historico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    servico_id = table.Column<long>(type: "bigint", nullable: false),
                    situacao_anterior = table.Column<string>(type: "text", nullable: true),
                    nova_situacao = table.Column<string>(type: "text", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    responsavel_id = table.Column<long>(type: "bigint", nullable: true),
                    responsavel_nome = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acompanhamento_servico_historico", x => x.id);
                    table.ForeignKey(
                        name: "FK_acompanhamento_servico_historico_acompanhamento_servicos_se~",
                        column: x => x.servico_id,
                        principalTable: "acompanhamento_servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "google_integration_audit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_id = table.Column<long>(type: "bigint", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    result_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_integration_audit", x => x.id);
                    table.ForeignKey(
                        name: "FK_google_integration_audit_google_integrations_integration_id",
                        column: x => x.integration_id,
                        principalTable: "google_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "google_integration_scopes",
                columns: table => new
                {
                    integration_id = table.Column<long>(type: "bigint", nullable: false),
                    scope = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_integration_scopes", x => new { x.integration_id, x.scope });
                    table.ForeignKey(
                        name: "FK_google_integration_scopes_google_integrations_integration_id",
                        column: x => x.integration_id,
                        principalTable: "google_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "google_sheet_report_metadata",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_id = table.Column<long>(type: "bigint", nullable: false),
                    report_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    spreadsheet_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    spreadsheet_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_range = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_google_sheet_report_metadata", x => x.id);
                    table.ForeignKey(
                        name: "FK_google_sheet_report_metadata_google_integrations_integratio~",
                        column: x => x.integration_id,
                        principalTable: "google_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cadastro_servicos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    cliente_id = table.Column<long>(type: "bigint", nullable: true),
                    orcamento_id = table.Column<long>(type: "bigint", nullable: true),
                    condicao_pagamento_id = table.Column<long>(type: "bigint", nullable: true),
                    tipo_servico = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subtipo = table.Column<string>(type: "text", nullable: true),
                    data_entrada = table.Column<DateOnly>(type: "date", nullable: true),
                    situacao_inicial = table.Column<string>(type: "text", nullable: true),
                    documento_empresa = table.Column<string>(type: "text", nullable: true),
                    razao_social_empresa = table.Column<string>(type: "text", nullable: true),
                    contato_empresa = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    endereco_empresa = table.Column<string>(type: "text", nullable: true),
                    endereco_servico = table.Column<string>(type: "text", nullable: true),
                    mesmo_endereco_empresa = table.Column<bool>(type: "boolean", nullable: false),
                    valor_contrato = table.Column<decimal>(type: "numeric", nullable: true),
                    data_contrato = table.Column<DateOnly>(type: "date", nullable: true),
                    nome_condicao_pagamento = table.Column<string>(type: "text", nullable: true),
                    valor_nota_fiscal = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cadastro_servicos", x => x.id);
                    table.ForeignKey(
                        name: "FK_cadastro_servicos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_cadastro_servicos_condicoes_pagamento_condicao_pagamento_id",
                        column: x => x.condicao_pagamento_id,
                        principalTable: "condicoes_pagamento",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_cadastro_servicos_orcamentos_orcamento_id",
                        column: x => x.orcamento_id,
                        principalTable: "orcamentos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    service_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(38,2)", precision: 38, scale: 2, nullable: true),
                    rule_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    reference_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_active = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_usuarios_user_id",
                        column: x => x.user_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_meta_integration_audit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_id = table.Column<long>(type: "bigint", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    result_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    actor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_meta_integration_audit", x => x.id);
                    table.ForeignKey(
                        name: "FK_whatsapp_meta_integration_audit_whatsapp_meta_integrations_~",
                        column: x => x.integration_id,
                        principalTable: "whatsapp_meta_integrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "acompanhamento_servico_pendencias",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    servico_id = table.Column<long>(type: "bigint", nullable: false),
                    situacao_config_id = table.Column<long>(type: "bigint", nullable: true),
                    pendencia_config_id = table.Column<long>(type: "bigint", nullable: true),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    concluida = table.Column<bool>(type: "boolean", nullable: false),
                    concluida_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acompanhamento_servico_pendencias", x => x.id);
                    table.ForeignKey(
                        name: "FK_acompanhamento_servico_pendencias_acompanhamento_servico_si~",
                        column: x => x.situacao_config_id,
                        principalTable: "acompanhamento_servico_situacao_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_acompanhamento_servico_pendencias_acompanhamento_servicos_s~",
                        column: x => x.servico_id,
                        principalTable: "acompanhamento_servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_acompanhamento_servico_pendencias_acompanhamento_situacao_p~",
                        column: x => x.pendencia_config_id,
                        principalTable: "acompanhamento_situacao_pendencias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cadastro_servico_parcelas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cadastro_servico_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_parcela = table.Column<int>(type: "integer", nullable: true),
                    valor = table.Column<decimal>(type: "numeric", nullable: true),
                    data_vencimento = table.Column<DateOnly>(type: "date", nullable: true),
                    forma_pagamento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cadastro_servico_parcelas", x => x.id);
                    table.ForeignKey(
                        name: "FK_cadastro_servico_parcelas_cadastro_servicos_cadastro_servic~",
                        column: x => x.cadastro_servico_id,
                        principalTable: "cadastro_servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cadastro_servico_prestadores",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cadastro_servico_id = table.Column<long>(type: "bigint", nullable: false),
                    prestador_id = table.Column<long>(type: "bigint", nullable: true),
                    nome_prestador = table.Column<string>(type: "text", nullable: true),
                    valor_provisionado = table.Column<decimal>(type: "numeric", nullable: true),
                    valor_efetivo = table.Column<decimal>(type: "numeric", nullable: true),
                    confirmado = table.Column<bool>(type: "boolean", nullable: true),
                    data_pagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    data_pagamento_tipo = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cadastro_servico_prestadores", x => x.id);
                    table.ForeignKey(
                        name: "FK_cadastro_servico_prestadores_cadastro_servicos_cadastro_ser~",
                        column: x => x.cadastro_servico_id,
                        principalTable: "cadastro_servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cadastro_servico_prestadores_prestadores_prestador_id",
                        column: x => x.prestador_id,
                        principalTable: "prestadores",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lancamentos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    origem = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    cadastro_servico_id = table.Column<long>(type: "bigint", nullable: true),
                    prestador_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo_servico = table.Column<string>(type: "text", nullable: true),
                    nome_cliente = table.Column<string>(type: "text", nullable: true),
                    nome_prestador = table.Column<string>(type: "text", nullable: true),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    valor = table.Column<decimal>(type: "numeric", nullable: true),
                    data = table.Column<DateOnly>(type: "date", nullable: true),
                    numero_parcela = table.Column<int>(type: "integer", nullable: true),
                    data_prevista_parcela = table.Column<DateOnly>(type: "date", nullable: true),
                    forma_pagamento = table.Column<string>(type: "text", nullable: true),
                    metodo_pagamento = table.Column<string>(type: "text", nullable: true),
                    plataforma = table.Column<string>(type: "text", nullable: true),
                    empresa = table.Column<string>(type: "text", nullable: true),
                    comprovante_url = table.Column<string>(type: "text", nullable: true),
                    comprovante_nome_arquivo = table.Column<string>(type: "text", nullable: true),
                    observacao = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_lancamentos_cadastro_servicos_cadastro_servico_id",
                        column: x => x.cadastro_servico_id,
                        principalTable: "cadastro_servicos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_lancamentos_prestadores_prestador_id",
                        column: x => x.prestador_id,
                        principalTable: "prestadores",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_acompanhamento_servico_historico_servico_id",
                table: "acompanhamento_servico_historico",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "idx_acompanhamento_pendencia_servico",
                table: "acompanhamento_servico_pendencias",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "idx_acompanhamento_pendencia_situacao",
                table: "acompanhamento_servico_pendencias",
                column: "situacao_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_acompanhamento_servico_pendencias_pendencia_config_id",
                table: "acompanhamento_servico_pendencias",
                column: "pendencia_config_id");

            migrationBuilder.CreateIndex(
                name: "uk_acompanhamento_situacao_tipo_nome",
                table: "acompanhamento_servico_situacao_config",
                columns: new[] { "tipo_servico", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_acompanhamento_servico_origem",
                table: "acompanhamento_servicos",
                columns: new[] { "tipo_servico", "origem_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_acompanhamento_pendencia_situacao_label",
                table: "acompanhamento_situacao_pendencias",
                columns: new[] { "situacao_config_id", "label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_avcbs_codigo",
                table: "avcbs",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cadastro_servico_parcelas_cadastro_servico_id",
                table: "cadastro_servico_parcelas",
                column: "cadastro_servico_id");

            migrationBuilder.CreateIndex(
                name: "IX_cadastro_servico_prestadores_cadastro_servico_id",
                table: "cadastro_servico_prestadores",
                column: "cadastro_servico_id");

            migrationBuilder.CreateIndex(
                name: "IX_cadastro_servico_prestadores_prestador_id",
                table: "cadastro_servico_prestadores",
                column: "prestador_id");

            migrationBuilder.CreateIndex(
                name: "IX_cadastro_servicos_cliente_id",
                table: "cadastro_servicos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_cadastro_servicos_codigo",
                table: "cadastro_servicos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cadastro_servicos_condicao_pagamento_id",
                table: "cadastro_servicos",
                column: "condicao_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_cadastro_servicos_orcamento_id",
                table: "cadastro_servicos",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_clcbs_codigo",
                table: "clcbs",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clientes_cnpj_cpf",
                table: "clientes",
                column: "cnpj_cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_condicoes_pagamento_nome",
                table: "condicoes_pagamento",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_google_integration_audit_integration_id",
                table: "google_integration_audit",
                column: "integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_google_integrations_integration_key",
                table: "google_integrations",
                column: "integration_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_google_sheet_report_name_integration",
                table: "google_sheet_report_metadata",
                columns: new[] { "integration_id", "report_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_cadastro_servico_id",
                table: "lancamentos",
                column: "cadastro_servico_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_codigo",
                table: "lancamentos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_prestador_id",
                table: "lancamentos",
                column: "prestador_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_obras_codigo",
                table: "obras",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_situacoes_label",
                table: "orcamento_situacoes",
                column: "label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamentos_codigo",
                table: "orcamentos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_pdf_template_key",
                table: "pdf_templates",
                column: "template_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_auth0_sub",
                table: "usuarios",
                column: "auth0_sub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_username",
                table: "usuarios",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_meta_integration_audit_integration_id",
                table: "whatsapp_meta_integration_audit",
                column: "integration_id");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_meta_integrations_integration_key",
                table: "whatsapp_meta_integrations",
                column: "integration_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acompanhamento_servico_historico");

            migrationBuilder.DropTable(
                name: "acompanhamento_servico_pendencias");

            migrationBuilder.DropTable(
                name: "avcbs");

            migrationBuilder.DropTable(
                name: "cadastro_servico_parcelas");

            migrationBuilder.DropTable(
                name: "cadastro_servico_prestadores");

            migrationBuilder.DropTable(
                name: "clcbs");

            migrationBuilder.DropTable(
                name: "custos_indiretos");

            migrationBuilder.DropTable(
                name: "google_integration_audit");

            migrationBuilder.DropTable(
                name: "google_integration_scopes");

            migrationBuilder.DropTable(
                name: "google_sheet_report_metadata");

            migrationBuilder.DropTable(
                name: "lancamentos");

            migrationBuilder.DropTable(
                name: "notification_rules");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "obras");

            migrationBuilder.DropTable(
                name: "orcamento_situacoes");

            migrationBuilder.DropTable(
                name: "pdf_templates");

            migrationBuilder.DropTable(
                name: "processos_adm");

            migrationBuilder.DropTable(
                name: "whatsapp_meta_integration_audit");

            migrationBuilder.DropTable(
                name: "acompanhamento_servicos");

            migrationBuilder.DropTable(
                name: "acompanhamento_situacao_pendencias");

            migrationBuilder.DropTable(
                name: "google_integrations");

            migrationBuilder.DropTable(
                name: "cadastro_servicos");

            migrationBuilder.DropTable(
                name: "prestadores");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "whatsapp_meta_integrations");

            migrationBuilder.DropTable(
                name: "acompanhamento_servico_situacao_config");

            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "condicoes_pagamento");

            migrationBuilder.DropTable(
                name: "orcamentos");
        }
    }
}
