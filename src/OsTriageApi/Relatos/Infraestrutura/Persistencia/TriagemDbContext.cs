using Microsoft.EntityFrameworkCore;

namespace OsTriageApi.Relatos.Infraestrutura.Persistencia;

public sealed class TriagemDbContext(DbContextOptions<TriagemDbContext> options) : DbContext(options)
{
    public DbSet<OrdemServicoEntity> OrdensServico => Set<OrdemServicoEntity>();
    public DbSet<SintomaEntity> Sintomas => Set<SintomaEntity>();
    public DbSet<ExecucaoAuditoriaEntity> LogsExecucoes => Set<ExecucaoAuditoriaEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarOrdem(modelBuilder);
        ConfigurarSintoma(modelBuilder);
        ConfigurarExecucao(modelBuilder);
    }

    private static void ConfigurarOrdem(ModelBuilder modelBuilder)
    {
        var entidade = modelBuilder.Entity<OrdemServicoEntity>();
        entidade.ToTable("ordens_servico", tabela =>
        {
            tabela.HasCheckConstraint("ck_ordens_canal", "canal_origem IN ('rest', 'telegram')");
            tabela.HasCheckConstraint("ck_ordens_ano", "ano IS NULL OR ano BETWEEN 1900 AND 2100");
            tabela.HasCheckConstraint("ck_ordens_categoria", "categoria IN ('corrente e relação', 'freios', 'suspensão', 'motor e lubrificação', 'elétrica e bateria', 'pneus e rodas', 'revisão periódica', 'outro ou não identificado')");
            tabela.HasCheckConstraint("ck_ordens_urgencia_llm", "urgencia_llm IN ('baixa', 'media', 'alta')");
            tabela.HasCheckConstraint("ck_ordens_urgencia_final", "urgencia_final IN ('baixa', 'media', 'alta')");
            tabela.HasCheckConstraint("ck_ordens_piso_urgencia", "urgencia_final = urgencia_llm OR urgencia_final = 'alta'");
            tabela.HasCheckConstraint("ck_ordens_risco", "NOT risco_detectado OR urgencia_final = 'alta'");
            tabela.HasCheckConstraint("ck_ordens_status", "status IN ('aberta', 'em_execucao', 'concluida', 'cancelada')");
        });
        entidade.HasKey(x => x.Id);
        entidade.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        entidade.Property(x => x.ChaveIdempotencia).HasColumnName("chave_idempotencia").HasMaxLength(100).IsRequired();
        entidade.HasIndex(x => x.ChaveIdempotencia).IsUnique().HasDatabaseName("uq_ordens_chave_idempotencia");
        entidade.Property(x => x.CanalOrigem).HasColumnName("canal_origem").HasMaxLength(20).IsRequired();
        entidade.Property(x => x.ModeloMoto).HasColumnName("modelo_moto").HasMaxLength(120).IsRequired(false);
        entidade.Property(x => x.Ano).HasColumnName("ano");
        entidade.Property(x => x.Categoria).HasColumnName("categoria").HasMaxLength(40).IsRequired();
        entidade.Property(x => x.UrgenciaLlm).HasColumnName("urgencia_llm").HasMaxLength(10).IsRequired();
        entidade.Property(x => x.UrgenciaFinal).HasColumnName("urgencia_final").HasMaxLength(10).IsRequired();
        entidade.Property(x => x.RiscoDetectado).HasColumnName("risco_detectado");
        entidade.Property(x => x.Prioridade).HasColumnName("prioridade").HasComputedColumnSql("CASE urgencia_final WHEN 'alta' THEN 3 WHEN 'media' THEN 2 ELSE 1 END", stored: true);
        entidade.Property(x => x.PecasProvaveis).HasColumnName("pecas_provaveis").HasColumnType("text[]").HasDefaultValueSql("'{}'::text[]");
        entidade.Property(x => x.CamposNaoConfirmados).HasColumnName("campos_nao_confirmados").HasColumnType("text[]").HasDefaultValueSql("'{}'::text[]");
        entidade.Property(x => x.MensagemCliente).HasColumnName("mensagem_cliente").HasMaxLength(1000);
        entidade.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("aberta").IsRequired();
        entidade.Property(x => x.DataCriacao).HasColumnName("data_criacao").HasDefaultValueSql("now()");
        entidade.HasIndex(x => new { x.Status, x.Prioridade, x.DataCriacao }).HasDatabaseName("ix_ordens_fila").IsDescending(false, true, false);
        entidade.HasIndex(x => new { x.Categoria, x.Status }).HasDatabaseName("ix_ordens_categoria_status");
    }

    private static void ConfigurarSintoma(ModelBuilder modelBuilder)
    {
        var entidade = modelBuilder.Entity<SintomaEntity>();
        entidade.ToTable("sintomas", tabela =>
        {
            tabela.HasCheckConstraint("ck_sintomas_posicao", "posicao >= 1");
            tabela.HasCheckConstraint("ck_sintomas_descricao", "char_length(btrim(descricao)) > 0");
        });
        entidade.HasKey(x => x.Id);
        entidade.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        entidade.Property(x => x.OrdemId).HasColumnName("ordem_id");
        entidade.Property(x => x.Posicao).HasColumnName("posicao");
        entidade.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(300).IsRequired();
        entidade.HasIndex(x => new { x.OrdemId, x.Posicao }).IsUnique().HasDatabaseName("uq_sintomas_ordem_posicao");
        entidade.HasOne(x => x.Ordem).WithMany(x => x.Sintomas).HasForeignKey(x => x.OrdemId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarExecucao(ModelBuilder modelBuilder)
    {
        var entidade = modelBuilder.Entity<ExecucaoAuditoriaEntity>();
        entidade.ToTable("logs_execucoes", tabela =>
        {
            tabela.HasCheckConstraint("ck_logs_canal", "canal_origem IN ('rest', 'telegram')");
            tabela.HasCheckConstraint("ck_logs_etapa", "etapa IN ('extracao', 'redacao_mensagem')");
            tabela.HasCheckConstraint("ck_logs_tentativa", "tentativa BETWEEN 1 AND 3");
            tabela.HasCheckConstraint("ck_logs_temperatura", "temperatura BETWEEN 0 AND 2");
            tabela.HasCheckConstraint("ck_logs_tempo", "tempo_ms >= 0");
            tabela.HasCheckConstraint("ck_logs_resultado", "resultado IN ('gravada', 'pergunta', 'falha_formato', 'falha_api', 'mensagem_gerada')");
            tabela.HasCheckConstraint("ck_logs_resultado_etapa", "etapa <> 'redacao_mensagem' OR resultado IN ('mensagem_gerada', 'falha_formato', 'falha_api')");
            tabela.HasCheckConstraint("ck_logs_gravada_com_ordem", "resultado <> 'gravada' OR ordem_id IS NOT NULL");
            tabela.HasCheckConstraint("ck_logs_hash", "relato_hash ~ '^[0-9a-f]{64}$'");
        });
        entidade.HasKey(x => x.IdExecucao);
        entidade.Property(x => x.IdExecucao).HasColumnName("id_execucao").HasDefaultValueSql("gen_random_uuid()");
        entidade.Property(x => x.IdPedido).HasColumnName("id_pedido");
        entidade.Property(x => x.OrdemId).HasColumnName("ordem_id");
        entidade.Property(x => x.CanalOrigem).HasColumnName("canal_origem").HasMaxLength(20).IsRequired();
        entidade.Property(x => x.Etapa).HasColumnName("etapa").HasMaxLength(20).IsRequired();
        entidade.Property(x => x.Tentativa).HasColumnName("tentativa");
        entidade.Property(x => x.DataHora).HasColumnName("data_hora").HasDefaultValueSql("now()");
        entidade.Property(x => x.VersaoPrompt).HasColumnName("versao_prompt").HasMaxLength(20).IsRequired();
        entidade.Property(x => x.ModeloLlm).HasColumnName("modelo_llm").HasMaxLength(60).IsRequired();
        entidade.Property(x => x.Temperatura).HasColumnName("temperatura").HasPrecision(3, 2);
        entidade.Property(x => x.TempoMs).HasColumnName("tempo_ms");
        entidade.Property(x => x.TokensEntrada).HasColumnName("tokens_entrada");
        entidade.Property(x => x.TokensSaida).HasColumnName("tokens_saida");
        entidade.Property(x => x.RelatoHash).HasColumnName("relato_hash").HasMaxLength(64).IsFixedLength().IsRequired();
        entidade.Property(x => x.RespostaBruta).HasColumnName("resposta_bruta");
        entidade.Property(x => x.RiscoDetectado).HasColumnName("risco_detectado");
        entidade.Property(x => x.Resultado).HasColumnName("resultado").HasMaxLength(20).IsRequired();
        entidade.Property(x => x.CodigoErro).HasColumnName("codigo_erro").HasMaxLength(60);
        entidade.HasOne<OrdemServicoEntity>().WithMany().HasForeignKey(x => x.OrdemId).OnDelete(DeleteBehavior.SetNull);
        entidade.HasIndex(x => x.IdPedido).HasDatabaseName("ix_logs_id_pedido");
        entidade.HasIndex(x => x.OrdemId).HasFilter("ordem_id IS NOT NULL").HasDatabaseName("ix_logs_ordem");
        entidade.HasIndex(x => new { x.VersaoPrompt, x.Etapa, x.Resultado }).HasDatabaseName("ix_logs_metricas");
        entidade.HasIndex(x => x.DataHora).HasDatabaseName("ix_logs_data_hora");
    }
}
