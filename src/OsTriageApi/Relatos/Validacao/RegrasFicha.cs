using OsTriageApi.Relatos.Modelos;

namespace OsTriageApi.Relatos.Validacao;

public static class RegrasFicha
{
    public static ResultadoRegrasFicha Avaliar(FichaExtraida ficha, string relato)
    {
        ArgumentNullException.ThrowIfNull(ficha);
        ArgumentNullException.ThrowIfNull(relato);

        var camposFaltando = new List<string>();

        if (string.IsNullOrWhiteSpace(ficha.ModeloMoto))
            camposFaltando.Add("modeloMoto");

        if (ficha.Sintomas is null || !ficha.Sintomas.Any(sintoma => !string.IsNullOrWhiteSpace(sintoma)))
            camposFaltando.Add("problema");

        var temRisco = RegrasUrgencia.BuscarExpressao(relato) is not null;
        var urgenciaFinal = temRisco ? UrgenciaRelato.Alta : ficha.Urgencia;

        return new ResultadoRegrasFicha(camposFaltando.Count == 0, camposFaltando, urgenciaFinal);
    }
}
