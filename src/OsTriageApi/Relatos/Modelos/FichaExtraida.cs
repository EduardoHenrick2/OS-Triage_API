namespace OsTriageApi.Relatos.Modelos;

public sealed record FichaExtraida(
    string? ModeloMoto,
    IReadOnlyList<string>? Sintomas,
    CategoriaRelato? Categoria,
    UrgenciaRelato? Urgencia,
    int? Ano = null,
    IReadOnlyList<string>? PecasProvaveis = null);
