using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OsTriageApi.Relatos.Validacao;

// Sinalizador de urgência: se a descrição contiver uma das 5 expressões de risco, a urgência mínima é Alta.
// Em "X ou Y", cada alternativa ativa a mesma expressão.
public static class RegrasUrgencia
{
    private static readonly (string Expressao, Regex Padrao)[] Padroes = ConstruirPadroes().ToArray();

    public static string? BuscarExpressao(string descricao)
    {
        var texto = Normalizar(descricao);

        foreach (var (expressao, padrao) in Padroes)
        {
            if (padrao.IsMatch(texto))
                return expressao;
        }

        return null;
    }

    // Sem acento, minúsculas e espaços simples. Assim "Direção" casa com "direcao".
    // O início da palavra é ancorado com \b, então "freio" também casa com "freios" e "pneu careca" com "pneus carecas".
    public static string Normalizar(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);

        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var semAcento = sb.ToString().ToLowerInvariant();
        return Regex.Replace(semAcento, @"\s+", " ").Trim();
    }

    private static IEnumerable<(string Expressao, Regex Padrao)> ConstruirPadroes()
    {
        (string Expressao, string[] Alternativas)[] definicoes =
        [
            ("freio", ["freio"]),
            ("vazamento ou perda de óleo", ["vazamento", "perda de óleo"]),
            ("folga na direção", ["folga na direção"]),
            ("pneu furado ou careca", ["pneu furado", "pneu careca"]),
            ("moto desligando sozinha", ["moto desligando sozinha"])
        ];

        foreach (var (expressao, alternativas) in definicoes)
        {
            foreach (var alternativa in alternativas)
            {
                var padrao = new Regex(
                    @"\b" + Regex.Escape(Normalizar(alternativa)),
                    RegexOptions.CultureInvariant | RegexOptions.Compiled);

                yield return (expressao, padrao);
            }
        }
    }
}
