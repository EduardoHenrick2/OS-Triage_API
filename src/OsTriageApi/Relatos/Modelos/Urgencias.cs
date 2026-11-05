namespace OsTriageApi.Relatos.Modelos;

public static class Urgencias
{
    public static string ToValorApi(this UrgenciaRelato urgencia) => urgencia switch
    {
        UrgenciaRelato.Baixa => "baixa",
        UrgenciaRelato.Media => "media",
        UrgenciaRelato.Alta => "alta",
        _ => throw new ArgumentOutOfRangeException(nameof(urgencia), urgencia, "Urgência inválida.")
    };
}
