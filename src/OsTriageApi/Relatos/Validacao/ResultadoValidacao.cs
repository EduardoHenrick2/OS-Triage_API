namespace OsTriageApi.Relatos.Validacao;

public sealed record ResultadoValidacao(
    bool Valido,
    IReadOnlyDictionary<string, string[]> Erros);
