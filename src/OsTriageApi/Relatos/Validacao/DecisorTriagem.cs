using OsTriageApi.Relatos.Modelos;

namespace OsTriageApi.Relatos.Validacao;

public static class DecisorTriagem
{
    public static DecisaoTriagem Avaliar(FichaExtraida ficha, string relato)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        ArgumentNullException.ThrowIfNull(relato);

        var anoConfirmado = ConfirmadorAno.Confirmar(ficha.Ano, relato);
        IReadOnlyList<string> camposNaoConfirmados = ficha.Ano is not null && anoConfirmado is null
            ? new[] { "ano" }
            : Array.Empty<string>();
        var fichaConfirmada = ficha with { Ano = anoConfirmado };
        var regras = RegrasFicha.Avaliar(fichaConfirmada, relato);

        return new DecisaoTriagem(fichaConfirmada, regras, camposNaoConfirmados);
    }
}

public sealed record DecisaoTriagem(
    FichaExtraida Ficha,
    ResultadoRegrasFicha Regras,
    IReadOnlyList<string> CamposNaoConfirmados);
