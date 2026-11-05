namespace OsTriageApi.Relatos.Infraestrutura.Persistencia;

public sealed class OrdemServicoEntity
{
    public Guid Id { get; set; }
    public required string ChaveIdempotencia { get; set; }
    public string CanalOrigem { get; set; } = "rest";
    public string? ModeloMoto { get; set; }
    public int? Ano { get; set; }
    public required string Categoria { get; set; }
    public required string UrgenciaLlm { get; set; }
    public required string UrgenciaFinal { get; set; }
    public bool RiscoDetectado { get; set; }
    public short Prioridade { get; private set; }
    public string[] PecasProvaveis { get; set; } = [];
    public string[] CamposNaoConfirmados { get; set; } = [];
    public string? MensagemCliente { get; set; }
    public string Status { get; set; } = "aberta";
    public DateTimeOffset DataCriacao { get; set; }
    public List<SintomaEntity> Sintomas { get; set; } = [];
}

public sealed class SintomaEntity
{
    public long Id { get; set; }
    public Guid OrdemId { get; set; }
    public short Posicao { get; set; }
    public required string Descricao { get; set; }
    public OrdemServicoEntity Ordem { get; set; } = null!;
}

public sealed class ExecucaoAuditoriaEntity
{
    public Guid IdExecucao { get; set; }
    public Guid IdPedido { get; set; }
    public Guid? OrdemId { get; set; }
    public string CanalOrigem { get; set; } = "rest";
    public required string Etapa { get; set; }
    public short Tentativa { get; set; }
    public DateTimeOffset DataHora { get; set; }
    public required string VersaoPrompt { get; set; }
    public required string ModeloLlm { get; set; }
    public decimal Temperatura { get; set; }
    public int TempoMs { get; set; }
    public int? TokensEntrada { get; set; }
    public int? TokensSaida { get; set; }
    public required string RelatoHash { get; set; }
    public string? RespostaBruta { get; set; }
    public bool RiscoDetectado { get; set; }
    public required string Resultado { get; set; }
    public string? CodigoErro { get; set; }
}
