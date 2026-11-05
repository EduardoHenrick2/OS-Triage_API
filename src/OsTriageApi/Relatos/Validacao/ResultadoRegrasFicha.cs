using OsTriageApi.Relatos.Modelos;

namespace OsTriageApi.Relatos.Validacao;

public sealed record ResultadoRegrasFicha(
    bool Gravavel,
    IReadOnlyList<string> CamposFaltando,
    UrgenciaRelato? UrgenciaFinal);
