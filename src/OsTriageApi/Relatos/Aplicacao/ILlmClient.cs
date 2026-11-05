namespace OsTriageApi.Relatos.Aplicacao;

public interface ILlmClient
{
    Task<RespostaLlm> ExtrairFichaAsync(string relato, CancellationToken cancellationToken);

    Task<RespostaLlm> GerarMensagemAsync(
        FichaParaMensagem ficha,
        CancellationToken cancellationToken);
}

public sealed record RespostaLlm(
    string Texto,
    string Modelo,
    decimal Temperatura,
    int TempoMs,
    int? TokensEntrada,
    int? TokensSaida);

public sealed record FichaParaMensagem(
    string ModeloMoto,
    IReadOnlyList<string> Sintomas,
    string Categoria,
    string UrgenciaFinal);
