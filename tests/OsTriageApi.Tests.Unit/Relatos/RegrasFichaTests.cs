using OsTriageApi.Relatos.Modelos;
using OsTriageApi.Relatos.Validacao;

namespace OsTriageApi.Tests.Unit.Relatos;

public sealed class RegrasFichaTests
{
    [Fact]
    public void Avaliar_CategoriaOutroComModeloESintoma_EGravavelSemDetalhe()
    {
        var ficha = new FichaExtraida(
            "Honda CG 160",
            ["serviço não identificado"],
            CategoriaRelato.OutroOuNaoIdentificado,
            UrgenciaRelato.Baixa);

        var resultado = RegrasFicha.Avaliar(ficha, "Honda CG 160 precisa de um serviço não identificado.");

        Assert.True(resultado.Gravavel);
        Assert.Empty(resultado.CamposFaltando);
        Assert.Equal(UrgenciaRelato.Baixa, resultado.UrgenciaFinal);
    }

    [Fact]
    public void Avaliar_SemModelo_PedeModeloMoto()
    {
        var ficha = new FichaExtraida(null, ["ruído no motor"], CategoriaRelato.MotorELubrificacao, UrgenciaRelato.Media);

        var resultado = RegrasFicha.Avaliar(ficha, "A moto apresenta ruído no motor.");

        Assert.False(resultado.Gravavel);
        Assert.Equal(new[] { "modeloMoto" }, resultado.CamposFaltando);
    }

    [Theory]
    [MemberData(nameof(SintomasAusentes))]
    public void Avaliar_SemSintomaComTexto_PedeProblema(IReadOnlyList<string>? sintomas)
    {
        var ficha = new FichaExtraida("Honda CG 160", sintomas, CategoriaRelato.OutroOuNaoIdentificado, UrgenciaRelato.Baixa);

        var resultado = RegrasFicha.Avaliar(ficha, "Honda CG 160.");

        Assert.False(resultado.Gravavel);
        Assert.Equal(new[] { "problema" }, resultado.CamposFaltando);
    }

    [Fact]
    public void Avaliar_ComRisco_ElevaUrgenciaParaAlta()
    {
        var ficha = new FichaExtraida("Honda CG 160", ["freio fraco"], CategoriaRelato.Freios, UrgenciaRelato.Baixa);

        var resultado = RegrasFicha.Avaliar(ficha, "Honda CG 160 com freio fraco.");

        Assert.Equal(UrgenciaRelato.Alta, resultado.UrgenciaFinal);
    }

    [Theory]
    [InlineData(UrgenciaRelato.Baixa)]
    [InlineData(UrgenciaRelato.Media)]
    [InlineData(UrgenciaRelato.Alta)]
    public void Avaliar_SemRisco_PreservaUrgenciaDoLlm(UrgenciaRelato urgencia)
    {
        var ficha = new FichaExtraida("Honda CG 160", ["ruído na corrente"], CategoriaRelato.CorrenteERelacao, urgencia);

        var resultado = RegrasFicha.Avaliar(ficha, "Honda CG 160 com ruído na corrente.");

        Assert.Equal(urgencia, resultado.UrgenciaFinal);
    }

    public static TheoryData<IReadOnlyList<string>?> SintomasAusentes => new()
    {
        null,
        Array.Empty<string>(),
        new[] { "", "   " }
    };
}
