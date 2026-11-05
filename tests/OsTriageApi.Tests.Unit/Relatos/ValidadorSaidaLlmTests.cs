using OsTriageApi.Relatos.Modelos;
using OsTriageApi.Relatos.Validacao;

namespace OsTriageApi.Tests.Unit.Relatos;

public sealed class ValidadorSaidaLlmTests
{
    [Fact]
    public void Validar_SaidaCompleta_NormalizaCategoriaEUrgencia()
    {
        var resultado = ValidadorSaidaLlm.Validar("""
            {
              "modeloMoto": "Honda CG 160",
              "ano": 2019,
              "sintomas": ["freio fraco"],
              "categoria": "  FREIOS ",
              "urgencia": "MÉDIA",
              "pecasProvaveis": ["pastilha"]
            }
            """);

        Assert.True(resultado.Valido);
        Assert.Equal(CategoriaRelato.Freios, resultado.Ficha?.Categoria);
        Assert.Equal(UrgenciaRelato.Media, resultado.Ficha?.Urgencia);
    }

    [Theory]
    [InlineData("categoria", "suspensão traseira", "categoria_invalida")]
    [InlineData("urgencia", "critica", "urgencia_invalida")]
    public void Validar_ValorForaDoDominio_Rejeita(string campo, string valor, string codigo)
    {
        var json = $$"""
            {
              "modeloMoto": "Honda CG 160",
              "ano": 2019,
              "sintomas": ["ruído"],
              "categoria": "{{(campo == "categoria" ? valor : "freios")}}",
              "urgencia": "{{(campo == "urgencia" ? valor : "media")}}",
              "pecasProvaveis": []
            }
            """;

        var resultado = ValidadorSaidaLlm.Validar(json);

        Assert.False(resultado.Valido);
        Assert.Equal(codigo, resultado.CodigoErro);
    }

    [Fact]
    public void Validar_CampoDesconhecido_Rejeita()
    {
        var resultado = ValidadorSaidaLlm.Validar("""
            {
              "modeloMoto": "Honda CG 160",
              "ano": 2019,
              "sintomas": ["ruído"],
              "categoria": "freios",
              "urgencia": "media",
              "pecasProvaveis": [],
              "explicacao": "texto extra"
            }
            """);

        Assert.False(resultado.Valido);
        Assert.Equal("campo_desconhecido:explicacao", resultado.CodigoErro);
    }

    [Fact]
    public void Validar_SemProblema_AceitaCategoriaEUrgenciaNulasParaPergunta()
    {
        var resultado = ValidadorSaidaLlm.Validar("""
            {
              "modeloMoto": "Honda CG 160",
              "ano": 2019,
              "sintomas": [],
              "categoria": null,
              "urgencia": null,
              "pecasProvaveis": []
            }
            """);

        Assert.True(resultado.Valido);
        Assert.Null(resultado.Ficha?.Categoria);
        Assert.Null(resultado.Ficha?.Urgencia);
    }
}
