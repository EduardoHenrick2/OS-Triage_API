using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OsTriageApi.Relatos.Modelos;

public static class Normalizador
{
    public static string Normalizar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var semAcentos = new StringBuilder(decomposto.Length);

        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                semAcentos.Append(caractere);
        }

        return Regex.Replace(semAcentos.ToString().ToLowerInvariant(), @"\s+", " ").Trim();
    }
}
