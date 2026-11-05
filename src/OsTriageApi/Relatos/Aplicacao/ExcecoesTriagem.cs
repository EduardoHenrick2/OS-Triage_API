namespace OsTriageApi.Relatos.Aplicacao;

public sealed class LlmIndisponivelException(
    string codigoErro,
    int retryAfterSegundos,
    Exception? innerException = null)
    : Exception("O serviço de IA está indisponível.", innerException)
{
    public string CodigoErro { get; } = codigoErro;
    public int RetryAfterSegundos { get; } = retryAfterSegundos;
}

public sealed class BancoIndisponivelException(Exception innerException)
    : Exception("O banco de dados está indisponível.", innerException);
