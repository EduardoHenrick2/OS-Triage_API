using System.Text.Json;
using OsTriageApi.Relatos.Configuracao;
using OsTriageApi.Relatos.Dtos;
using OsTriageApi.Relatos.Validacao;

namespace OsTriageApi.Tests.Unit.Relatos;

public sealed class ContratoRelatoTests
{
    private static JsonSerializerOptions CriarOpcoes()
    {
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        OpcoesJsonRelato.Configurar(opcoes);
        return opcoes;
    }

    [Fact]
    public void Desserializar_ComSomenteRelato_Aceita()
    {
        var entrada = JsonSerializer.Deserialize<CriarRelatoRequest>(
            """{"relato":"Honda CG 160 com ruído na corrente."}""",
            CriarOpcoes());

        Assert.Equal("Honda CG 160 com ruído na corrente.", entrada?.Relato);
    }

    [Theory]
    [InlineData("""{"relato":"Honda CG 160 com freio fraco.","urgenciaFinal":"baixa"}""")]
    [InlineData("""{"descricao":"Honda CG 160 com freio fraco."}""")]
    public void Desserializar_ComCampoDesconhecido_Rejeita(string json)
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CriarRelatoRequest>(json, CriarOpcoes()));
    }

    [Fact]
    public void Desserializar_JsonMalformado_Rejeita()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CriarRelatoRequest>("{\"relato\":\"Honda CG 160\"", CriarOpcoes()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validar_RelatoAusenteOuVazio_RetornaErroNoCampoRelato(string? relato)
    {
        var resultado = RelatoValidador.Validar(new CriarRelatoRequest(relato), 2_000);

        Assert.False(resultado.Valido);
        Assert.True(resultado.Erros.ContainsKey("relato"));
    }

    [Fact]
    public void Validar_RelatoComDoisMilCaracteres_Aceita()
    {
        var resultado = RelatoValidador.Validar(new CriarRelatoRequest(new string('a', 2_000)), 2_000);

        Assert.True(resultado.Valido);
        Assert.Empty(resultado.Erros);
    }

    [Fact]
    public void Validar_RespeitaLimiteConfigurado()
    {
        var resultado = RelatoValidador.Validar(new CriarRelatoRequest("123456"), 5);

        Assert.False(resultado.Valido);
        Assert.True(resultado.Erros.ContainsKey("relato"));
    }

    [Fact]
    public void Validar_RelatoAcimaDoLimite_RetornaErroNoCampoRelato()
    {
        var resultado = RelatoValidador.Validar(new CriarRelatoRequest(new string('a', 2_001)), 2_000);

        Assert.False(resultado.Valido);
        Assert.True(resultado.Erros.ContainsKey("relato"));
    }
}
