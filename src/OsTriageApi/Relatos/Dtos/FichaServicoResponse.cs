namespace OsTriageApi.Relatos.Dtos;

public sealed record FichaServicoResponse(
    Guid IdPedido,
    Guid? IdOrdem,
    string Resultado,
    string? ModeloMoto,
    int? Ano,
    IReadOnlyList<string> Sintomas,
    string? Categoria,
    string? UrgenciaLlm,
    string? UrgenciaFinal,
    IReadOnlyList<string> PecasProvaveis,
    bool AvisoUrgencia,
    IReadOnlyList<string> CamposFaltando,
    string? PerguntaAoAtendente,
    IReadOnlyList<string> CamposNaoConfirmados,
    string? MensagemCliente,
    IReadOnlyList<string> Avisos);
