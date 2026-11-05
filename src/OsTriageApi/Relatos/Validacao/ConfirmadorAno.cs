using System.Text.RegularExpressions;

namespace OsTriageApi.Relatos.Validacao;

public static partial class ConfirmadorAno
{
    public static int? Confirmar(int? ano, string relato)
    {
        if (ano is null)
            return null;

        return Regex.IsMatch(relato, $@"(?<!\d){ano.Value}(?!\d)", RegexOptions.CultureInvariant)
            ? ano
            : null;
    }
}
