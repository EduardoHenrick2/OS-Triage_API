using OsTriageApi.Relatos.Modelos;

namespace OsTriageApi.Tests.Unit.Relatos;

public sealed class CategoriasCanonicasTests
{
    [Theory]
    [InlineData("corrente e relação", CategoriaRelato.CorrenteERelacao, "corrente e relação")]
    [InlineData(" FREIOS ", CategoriaRelato.Freios, "freios")]
    [InlineData("SUSPENSAO", CategoriaRelato.Suspensao, "suspensão")]
    [InlineData("motor   e lubrificação", CategoriaRelato.MotorELubrificacao, "motor e lubrificação")]
    [InlineData("ELÉTRICA E BATERIA", CategoriaRelato.EletricaEBateria, "elétrica e bateria")]
    [InlineData("pneus e rodas", CategoriaRelato.PneusERodas, "pneus e rodas")]
    [InlineData("revisao periodica", CategoriaRelato.RevisaoPeriodica, "revisão periódica")]
    [InlineData("OUTRO OU NAO IDENTIFICADO", CategoriaRelato.OutroOuNaoIdentificado, "outro ou não identificado")]
    public void TryObter_NormalizaEntradaEDevolveFormaCanonica(
        string entrada,
        CategoriaRelato categoriaEsperada,
        string formaCanonica)
    {
        Assert.True(CategoriasCanonicas.TryObter(entrada, out var categoria));
        Assert.Equal(categoriaEsperada, categoria);
        Assert.Equal(formaCanonica, categoria.ToCanonica());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("automóvel")]
    public void TryObter_ValorInvalido_RetornaFalse(string? entrada)
    {
        Assert.False(CategoriasCanonicas.TryObter(entrada, out _));
    }
}
