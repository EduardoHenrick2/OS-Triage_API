using OsTriageApi.Relatos.Dtos;

namespace OsTriageApi.Relatos.Aplicacao;

public enum TipoResultadoTriagem
{
    Gravada,
    Pergunta,
    SaidaLlmInvalida,
    IaIndisponivel,
    BancoIndisponivel
}

public sealed record ResultadoTriagem(
    TipoResultadoTriagem Tipo,
    Guid IdPedido,
    FichaServicoResponse? Ficha = null,
    bool UrgenciaRiscoDetectado = false,
    int? RetryAfterSegundos = null);
