namespace OsTriageApi.Relatos.Modelos;

// Mapeamento entre o valor canônico do gabarito (minúsculas, com acentos) e o enum interno.
// A busca é ordinal e sensível a maiúsculas: "Freios" não é aceito, apenas "freios".
public static class CategoriasCanonicas
{
    private static readonly IReadOnlyDictionary<string, CategoriaRelato> Mapa =
        new Dictionary<string, CategoriaRelato>(StringComparer.Ordinal)
        {
            ["corrente e relação"] = CategoriaRelato.CorrenteERelacao,
            ["freios"] = CategoriaRelato.Freios,
            ["suspensão"] = CategoriaRelato.Suspensao,
            ["motor e lubrificação"] = CategoriaRelato.MotorELubrificacao,
            ["elétrica e bateria"] = CategoriaRelato.EletricaEBateria,
            ["pneus e rodas"] = CategoriaRelato.PneusERodas,
            ["revisão periódica"] = CategoriaRelato.RevisaoPeriodica,
            ["outro ou não identificado"] = CategoriaRelato.OutroOuNaoIdentificado
        };

    public static IEnumerable<string> Valores => Mapa.Keys;

    public static bool TryObter(string valor, out CategoriaRelato categoria) =>
        Mapa.TryGetValue(valor, out categoria);
}
