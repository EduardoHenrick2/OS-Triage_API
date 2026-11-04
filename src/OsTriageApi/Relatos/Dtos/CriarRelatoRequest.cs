namespace OsTriageApi.Relatos.Dtos;

// Entrada bruta da API. Todos os campos são anuláveis para que a validação, e não a
// desserialização, seja a única responsável por rejeitar dados ausentes ou inválidos.
public sealed record CriarRelatoRequest(
    string? Descricao,
    string? Categoria,
    string? DetalheCategoria,
    string? ModeloMoto);
