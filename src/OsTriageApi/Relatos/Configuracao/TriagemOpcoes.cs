namespace OsTriageApi.Relatos.Configuracao;

public sealed class TriagemOpcoes
{
    public const string Secao = "Triagem";

    public int TamanhoMaximoRelato { get; init; } = 2_000;
    public int MaximoTentativasExtracao { get; init; } = 3;
    public int MaximoTentativasMensagem { get; init; } = 3;
    public int LimiteMensagemPalavras { get; init; } = 60;
}
