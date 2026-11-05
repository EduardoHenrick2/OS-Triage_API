using System.Text.Json;
using OsTriageApi.Relatos.Modelos;

namespace OsTriageApi.Relatos.Validacao;

public static class ValidadorSaidaLlm
{
    private static readonly HashSet<string> CamposEsperados =
    [
        "modeloMoto", "ano", "sintomas", "categoria", "urgencia", "pecasProvaveis"
    ];

    public static ResultadoValidacaoSaida Validar(string texto)
    {
        try
        {
            using var documento = JsonDocument.Parse(texto);
            if (documento.RootElement.ValueKind != JsonValueKind.Object)
                return Invalido("nao_e_objeto");

            var raiz = documento.RootElement;
            var nomes = raiz.EnumerateObject().Select(propriedade => propriedade.Name).ToArray();
            var desconhecido = nomes.FirstOrDefault(nome => !CamposEsperados.Contains(nome));
            if (desconhecido is not null)
                return Invalido($"campo_desconhecido:{desconhecido}");

            var ausente = CamposEsperados.FirstOrDefault(campo => !raiz.TryGetProperty(campo, out _));
            if (ausente is not null)
                return Invalido($"campo_ausente:{ausente}");

            if (!TryLerStringNula(raiz.GetProperty("modeloMoto"), out var modelo))
                return Invalido("tipo_invalido:modeloMoto");

            if (!TryLerAno(raiz.GetProperty("ano"), out var ano))
                return Invalido("ano_invalido");

            if (!TryLerLista(raiz.GetProperty("sintomas"), out var sintomas))
                return Invalido("tipo_invalido:sintomas");

            if (!TryLerLista(raiz.GetProperty("pecasProvaveis"), out var pecas))
                return Invalido("tipo_invalido:pecasProvaveis");

            CategoriaRelato? categoria = null;
            var categoriaJson = raiz.GetProperty("categoria");
            if (categoriaJson.ValueKind == JsonValueKind.String)
            {
                if (!CategoriasCanonicas.TryObter(categoriaJson.GetString(), out var categoriaLida))
                    return Invalido("categoria_invalida");
                categoria = categoriaLida;
            }
            else if (categoriaJson.ValueKind != JsonValueKind.Null)
            {
                return Invalido("tipo_invalido:categoria");
            }

            UrgenciaRelato? urgencia = null;
            var urgenciaJson = raiz.GetProperty("urgencia");
            if (urgenciaJson.ValueKind == JsonValueKind.String)
            {
                urgencia = Normalizador.Normalizar(urgenciaJson.GetString() ?? string.Empty) switch
                {
                    "baixa" => UrgenciaRelato.Baixa,
                    "media" => UrgenciaRelato.Media,
                    "alta" => UrgenciaRelato.Alta,
                    _ => null
                };
                if (urgencia is null)
                    return Invalido("urgencia_invalida");
            }
            else if (urgenciaJson.ValueKind != JsonValueKind.Null)
            {
                return Invalido("tipo_invalido:urgencia");
            }

            var temProblema = sintomas.Any(sintoma => !string.IsNullOrWhiteSpace(sintoma));
            if (temProblema && categoria is null)
                return Invalido("campo_ausente:categoria");
            if (temProblema && urgencia is null)
                return Invalido("campo_ausente:urgencia");

            return new ResultadoValidacaoSaida(
                true,
                new FichaExtraida(modelo, sintomas, categoria, urgencia, ano, pecas),
                null);
        }
        catch (JsonException)
        {
            return Invalido("json_invalido");
        }
    }

    private static bool TryLerStringNula(JsonElement elemento, out string? valor)
    {
        if (elemento.ValueKind == JsonValueKind.Null)
        {
            valor = null;
            return true;
        }

        if (elemento.ValueKind == JsonValueKind.String)
        {
            valor = elemento.GetString()?.Trim();
            valor = string.IsNullOrWhiteSpace(valor) ? null : valor;
            return true;
        }

        valor = null;
        return false;
    }

    private static bool TryLerAno(JsonElement elemento, out int? ano)
    {
        if (elemento.ValueKind == JsonValueKind.Null)
        {
            ano = null;
            return true;
        }

        if (elemento.ValueKind == JsonValueKind.Number && elemento.TryGetInt32(out var valor) && valor is >= 1900 and <= 2100)
        {
            ano = valor;
            return true;
        }

        ano = null;
        return false;
    }

    private static bool TryLerLista(JsonElement elemento, out IReadOnlyList<string> valores)
    {
        if (elemento.ValueKind != JsonValueKind.Array)
        {
            valores = [];
            return false;
        }

        var lista = new List<string>();
        foreach (var item in elemento.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                valores = [];
                return false;
            }

            lista.Add(item.GetString()?.Trim() ?? string.Empty);
        }

        valores = lista;
        return true;
    }

    private static ResultadoValidacaoSaida Invalido(string codigo) => new(false, null, codigo);
}

public sealed record ResultadoValidacaoSaida(
    bool Valido,
    FichaExtraida? Ficha,
    string? CodigoErro);
