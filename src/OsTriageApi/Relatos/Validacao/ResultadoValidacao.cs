using OsTriageApi.Relatos.Modelos;

namespace OsTriageApi.Relatos.Validacao;

// Relato só é preenchido quando Valido é true.
// UrgenciaMinima e ExpressaoDeRisco são calculadas mesmo quando há erros de campo,
// para que um relato com risco não perca a escalação por estar mal preenchido.
public sealed record ResultadoValidacao(
    bool Valido,
    IReadOnlyList<string> Erros,
    UrgenciaRelato UrgenciaMinima,
    string? ExpressaoDeRisco,
    Relato? Relato);
