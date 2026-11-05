using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OsTriageApi.Relatos.Infraestrutura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ordens_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    chave_idempotencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    canal_origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    modelo_moto = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ano = table.Column<int>(type: "integer", nullable: true),
                    categoria = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    urgencia_llm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    urgencia_final = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    risco_detectado = table.Column<bool>(type: "boolean", nullable: false),
                    prioridade = table.Column<short>(type: "smallint", nullable: false, computedColumnSql: "CASE urgencia_final WHEN 'alta' THEN 3 WHEN 'media' THEN 2 ELSE 1 END", stored: true),
                    pecas_provaveis = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    campos_nao_confirmados = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    mensagem_cliente = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "aberta"),
                    data_criacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordens_servico", x => x.id);
                    table.CheckConstraint("ck_ordens_ano", "ano IS NULL OR ano BETWEEN 1900 AND 2100");
                    table.CheckConstraint("ck_ordens_canal", "canal_origem IN ('rest', 'telegram')");
                    table.CheckConstraint("ck_ordens_categoria", "categoria IN ('corrente e relação', 'freios', 'suspensão', 'motor e lubrificação', 'elétrica e bateria', 'pneus e rodas', 'revisão periódica', 'outro ou não identificado')");
                    table.CheckConstraint("ck_ordens_piso_urgencia", "urgencia_final = urgencia_llm OR urgencia_final = 'alta'");
                    table.CheckConstraint("ck_ordens_risco", "NOT risco_detectado OR urgencia_final = 'alta'");
                    table.CheckConstraint("ck_ordens_status", "status IN ('aberta', 'em_execucao', 'concluida', 'cancelada')");
                    table.CheckConstraint("ck_ordens_urgencia_final", "urgencia_final IN ('baixa', 'media', 'alta')");
                    table.CheckConstraint("ck_ordens_urgencia_llm", "urgencia_llm IN ('baixa', 'media', 'alta')");
                });

            migrationBuilder.CreateTable(
                name: "logs_execucoes",
                columns: table => new
                {
                    id_execucao = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    id_pedido = table.Column<Guid>(type: "uuid", nullable: false),
                    ordem_id = table.Column<Guid>(type: "uuid", nullable: true),
                    canal_origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    etapa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tentativa = table.Column<short>(type: "smallint", nullable: false),
                    data_hora = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    versao_prompt = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    modelo_llm = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    temperatura = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    tempo_ms = table.Column<int>(type: "integer", nullable: false),
                    tokens_entrada = table.Column<int>(type: "integer", nullable: true),
                    tokens_saida = table.Column<int>(type: "integer", nullable: true),
                    relato_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    resposta_bruta = table.Column<string>(type: "text", nullable: true),
                    risco_detectado = table.Column<bool>(type: "boolean", nullable: false),
                    resultado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    codigo_erro = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs_execucoes", x => x.id_execucao);
                    table.CheckConstraint("ck_logs_canal", "canal_origem IN ('rest', 'telegram')");
                    table.CheckConstraint("ck_logs_etapa", "etapa IN ('extracao', 'redacao_mensagem')");
                    table.CheckConstraint("ck_logs_gravada_com_ordem", "resultado <> 'gravada' OR ordem_id IS NOT NULL");
                    table.CheckConstraint("ck_logs_hash", "relato_hash ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_logs_resultado", "resultado IN ('gravada', 'pergunta', 'falha_formato', 'falha_api', 'mensagem_gerada')");
                    table.CheckConstraint("ck_logs_resultado_etapa", "etapa <> 'redacao_mensagem' OR resultado IN ('mensagem_gerada', 'falha_formato', 'falha_api')");
                    table.CheckConstraint("ck_logs_temperatura", "temperatura BETWEEN 0 AND 2");
                    table.CheckConstraint("ck_logs_tempo", "tempo_ms >= 0");
                    table.CheckConstraint("ck_logs_tentativa", "tentativa BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_logs_execucoes_ordens_servico_ordem_id",
                        column: x => x.ordem_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "sintomas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ordem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    posicao = table.Column<short>(type: "smallint", nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sintomas", x => x.id);
                    table.CheckConstraint("ck_sintomas_descricao", "char_length(btrim(descricao)) > 0");
                    table.CheckConstraint("ck_sintomas_posicao", "posicao >= 1");
                    table.ForeignKey(
                        name: "FK_sintomas_ordens_servico_ordem_id",
                        column: x => x.ordem_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_logs_data_hora",
                table: "logs_execucoes",
                column: "data_hora");

            migrationBuilder.CreateIndex(
                name: "ix_logs_id_pedido",
                table: "logs_execucoes",
                column: "id_pedido");

            migrationBuilder.CreateIndex(
                name: "ix_logs_metricas",
                table: "logs_execucoes",
                columns: new[] { "versao_prompt", "etapa", "resultado" });

            migrationBuilder.CreateIndex(
                name: "ix_logs_ordem",
                table: "logs_execucoes",
                column: "ordem_id",
                filter: "ordem_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_categoria_status",
                table: "ordens_servico",
                columns: new[] { "categoria", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ordens_fila",
                table: "ordens_servico",
                columns: new[] { "status", "prioridade", "data_criacao" },
                descending: new[] { false, true, false });

            migrationBuilder.CreateIndex(
                name: "uq_ordens_chave_idempotencia",
                table: "ordens_servico",
                column: "chave_idempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_sintomas_ordem_posicao",
                table: "sintomas",
                columns: new[] { "ordem_id", "posicao" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logs_execucoes");

            migrationBuilder.DropTable(
                name: "sintomas");

            migrationBuilder.DropTable(
                name: "ordens_servico");
        }
    }
}
