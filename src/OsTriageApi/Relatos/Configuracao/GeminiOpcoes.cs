namespace OsTriageApi.Relatos.Configuracao;

public sealed class GeminiOpcoes
{
    public const string Secao = "Gemini";

    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/";
    public string Modelo { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string VersaoPrompt { get; init; } = "v1";
    public decimal Temperatura { get; init; } = 0.2m;
    public int TimeoutSegundos { get; init; } = 10;
}
