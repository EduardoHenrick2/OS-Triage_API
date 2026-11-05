namespace OsTriageApi.Relatos.Validacao;

public static class LimitadorPalavras
{
    public static int Contar(string texto) =>
        texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    public static bool DentroDoLimite(string texto, int limite) => Contar(texto) <= limite;
}
