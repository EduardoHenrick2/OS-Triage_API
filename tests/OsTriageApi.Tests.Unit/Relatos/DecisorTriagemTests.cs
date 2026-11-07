using OsTriageApi.Relatos.Modelos;
using OsTriageApi.Relatos.Validacao;

namespace OsTriageApi.Tests.Unit.Relatos;

public sealed class DecisorTriagemTests
{
    [Fact]
    public void Avaliar_AnoForaDoRelato_RemoveAnoERegistraCampo()
    {
        var ficha = new FichaExtraida(
            "Yamaha Fazer 250",
            new[] { "ruído na corrente" },
            CategoriaRelato.CorrenteERelacao,
            UrgenciaRelato.Media,
            2019);

        var decisao = DecisorTriagem.Avaliar(ficha, "Yamaha Fazer 250 com ruído na corrente.");

        Assert.Null(decisao.Ficha.Ano);
        Assert.Equal(new[] { "ano" }, decisao.CamposNaoConfirmados);
        Assert.True(decisao.Regras.Gravavel);
    }

    [Fact]
    public void Avaliar_RiscoElevaUrgenciaSemAlterarUrgenciaExtraida()
    {
        var ficha = new FichaExtraida(
            "Honda CG 160",
            new[] { "freio fraco" },
            CategoriaRelato.Freios,
            UrgenciaRelato.Baixa);

        var decisao = DecisorTriagem.Avaliar(ficha, "Honda CG 160 com freio fraco.");

        Assert.Equal(UrgenciaRelato.Baixa, decisao.Ficha.Urgencia);
        Assert.Equal(UrgenciaRelato.Alta, decisao.Regras.UrgenciaFinal);
    }
}
