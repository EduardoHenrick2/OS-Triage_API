namespace OsTriageApi.Relatos.Modelos;

public sealed record Relato(
    string Texto,
    string ModeloMoto,
    IReadOnlyList<string> Sintomas,
    CategoriaRelato Categoria,
    UrgenciaRelato Urgencia,
    string? ExpressaoDeRisco);
