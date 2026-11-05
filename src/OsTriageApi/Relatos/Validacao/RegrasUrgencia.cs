using System.Text.RegularExpressions;
using OsTriageApi.Relatos.Modelos;

namespace OsTriageApi.Relatos.Validacao;

public static class RegrasUrgencia
{
    private static readonly (string Expressao, Regex Padrao)[] Padroes =
    [
        ("freio", CriarPadrao(@"\bfreios?\b")),
        ("vazamento de óleo", CriarPadrao(@"\bvazamento de oleo\b")),
        ("perda de óleo", CriarPadrao(@"\bperda de oleo\b")),
        ("folga na direção", CriarPadrao(@"\bfolga na direcao\b")),
        ("pneu furado ou careca", CriarPadrao(@"\bpneu (furado|careca)\b")),
        ("moto desligando sozinha", CriarPadrao(@"\bmoto desligando sozinha\b"))
    ];

    public static string? BuscarExpressao(string texto)
    {
        var normalizado = Normalizador.Normalizar(texto);
        return Padroes.FirstOrDefault(item => item.Padrao.IsMatch(normalizado)).Expressao;
    }

    private static Regex CriarPadrao(string expressao) =>
        new(expressao, RegexOptions.Compiled | RegexOptions.CultureInvariant);
}
