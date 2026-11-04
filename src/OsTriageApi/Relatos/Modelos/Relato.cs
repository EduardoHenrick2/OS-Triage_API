namespace OsTriageApi.Relatos.Modelos;

// Modelo de domínio de um relato já validado. Não é exposto diretamente pela API.
public sealed record Relato(
    string Descricao,
    CategoriaRelato Categoria,
    string? DetalheCategoria,
    string? ModeloMoto,
    UrgenciaRelato Urgencia,
    string? ExpressaoDeRisco);
