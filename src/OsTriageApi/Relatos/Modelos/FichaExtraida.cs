namespace OsTriageApi.Relatos.Modelos;

public sealed record FichaExtraida(
    string? ModeloMoto,
    IReadOnlyList<string>? Sintomas,
    CategoriaRelato Categoria,
    UrgenciaRelato Urgencia);
