using OsTriageApi.Relatos.Dtos;

namespace OsTriageApi.Relatos.Aplicacao;

public sealed record OrdemParaGravar(
    Guid IdPedido,
    string ChaveIdempotencia,
    string ModeloMoto,
    int? Ano,
    string Categoria,
    string UrgenciaLlm,
    string UrgenciaFinal,
    bool RiscoDetectado,
    IReadOnlyList<string> Sintomas,
    IReadOnlyList<string> PecasProvaveis,
    IReadOnlyList<string> CamposNaoConfirmados);

public sealed record ExecucaoLlm(
    Guid IdExecucao,
    Guid IdPedido,
    Guid? OrdemId,
    string Etapa,
    short Tentativa,
    string VersaoPrompt,
    string ModeloLlm,
    decimal Temperatura,
    int TempoMs,
    int? TokensEntrada,
    int? TokensSaida,
    string RelatoHash,
    string? RespostaBruta,
    bool RiscoDetectado,
    string Resultado,
    string? CodigoErro);

public interface ITriagemRepository
{
    Task<FichaServicoResponse?> BuscarPorChaveAsync(string chaveIdempotencia, CancellationToken cancellationToken);

    Task<FichaServicoResponse> GravarAsync(
        OrdemParaGravar ordem,
        ExecucaoLlm execucao,
        CancellationToken cancellationToken);

    Task RegistrarExecucaoAsync(ExecucaoLlm execucao, CancellationToken cancellationToken);

    Task AtualizarMensagemAsync(Guid ordemId, string? mensagem, CancellationToken cancellationToken);
}

public interface IRelatoHasher
{
    string Calcular(string relato);
}
