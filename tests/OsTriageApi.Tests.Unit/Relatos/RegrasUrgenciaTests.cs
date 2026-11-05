using OsTriageApi.Relatos.Validacao;

namespace OsTriageApi.Tests.Unit.Relatos;

public sealed class RegrasUrgenciaTests
{
    [Theory]
    [InlineData("A moto está com o freio fraco.", "freio")]
    [InlineData("Os FREIOS falharam.", "freio")]
    [InlineData("Existe vazamento de óleo.", "vazamento de óleo")]
    [InlineData("Notei perda de oleo.", "perda de óleo")]
    [InlineData("Há folga na direção.", "folga na direção")]
    [InlineData("O pneu furado precisa de troca.", "pneu furado ou careca")]
    [InlineData("O pneu careca perdeu aderência.", "pneu furado ou careca")]
    [InlineData("A moto desligando sozinha assusta.", "moto desligando sozinha")]
    public void BuscarExpressao_PadraoDaLinhaDeBase_RetornaExpressao(string relato, string esperada)
    {
        Assert.Equal(esperada, RegrasUrgencia.BuscarExpressao(relato));
    }

    [Theory]
    [InlineData("Preciso trocar o óleo na revisão.")]
    [InlineData("Quero fazer a revisão periódica.")]
    [InlineData("O pneu é novo e está sem careca.")]
    [InlineData("Existe vazamento de ar no pneu.")]
    public void BuscarExpressao_TextoSemPadraoDeRisco_NaoDetecta(string relato)
    {
        Assert.Null(RegrasUrgencia.BuscarExpressao(relato));
    }

    [Theory]
    [InlineData("O pneu traseiro está careca.")]
    [InlineData("O motor vaza óleo.")]
    public void BuscarExpressao_LacunaDaPropostaP1_NaoDetecta(string relato)
    {
        Assert.Null(RegrasUrgencia.BuscarExpressao(relato));
    }
}
