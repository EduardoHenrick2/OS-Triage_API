namespace OsTriageApi.Relatos.Modelos;

public static class CategoriasCanonicas
{
    private static readonly IReadOnlyDictionary<string, CategoriaRelato> Mapa =
        new Dictionary<string, CategoriaRelato>(StringComparer.Ordinal)
        {
            [Normalizador.Normalizar("corrente e relação")] = CategoriaRelato.CorrenteERelacao,
            [Normalizador.Normalizar("freios")] = CategoriaRelato.Freios,
            [Normalizador.Normalizar("suspensão")] = CategoriaRelato.Suspensao,
            [Normalizador.Normalizar("motor e lubrificação")] = CategoriaRelato.MotorELubrificacao,
            [Normalizador.Normalizar("elétrica e bateria")] = CategoriaRelato.EletricaEBateria,
            [Normalizador.Normalizar("pneus e rodas")] = CategoriaRelato.PneusERodas,
            [Normalizador.Normalizar("revisão periódica")] = CategoriaRelato.RevisaoPeriodica,
            [Normalizador.Normalizar("outro ou não identificado")] = CategoriaRelato.OutroOuNaoIdentificado
        };

    public static bool TryObter(string? valor, out CategoriaRelato categoria)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            categoria = default;
            return false;
        }

        return Mapa.TryGetValue(Normalizador.Normalizar(valor), out categoria);
    }

    public static string ToCanonica(this CategoriaRelato categoria) => categoria switch
    {
        CategoriaRelato.CorrenteERelacao => "corrente e relação",
        CategoriaRelato.Freios => "freios",
        CategoriaRelato.Suspensao => "suspensão",
        CategoriaRelato.MotorELubrificacao => "motor e lubrificação",
        CategoriaRelato.EletricaEBateria => "elétrica e bateria",
        CategoriaRelato.PneusERodas => "pneus e rodas",
        CategoriaRelato.RevisaoPeriodica => "revisão periódica",
        CategoriaRelato.OutroOuNaoIdentificado => "outro ou não identificado",
        _ => throw new ArgumentOutOfRangeException(nameof(categoria), categoria, "Categoria inválida.")
    };
}
