using System.Text.Json;
using Microsoft.Extensions.Options;
using OsTriageApi.Relatos.Aplicacao;
using OsTriageApi.Relatos.Configuracao;
using OsTriageApi.Relatos.Dtos;
using OsTriageApi.Relatos.Validacao;

namespace OsTriageApi.Endpoints;

public static class TriagemEndpoints
{
    public static IEndpointRouteBuilder MapTriagemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/triagem", ProcessarAsync)
            .Accepts<CriarRelatoRequest>("application/json")
            .Produces<FichaServicoResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .Produces<FichaServicoResponse>(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        return endpoints;
    }

    private static async Task<IResult> ProcessarAsync(
        HttpContext httpContext,
        IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> opcoesJson,
        IOptions<TriagemOpcoes> opcoesTriagem,
        TriagemService triagemService,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.HasJsonContentType())
            return Results.Problem(title: "Content-Type não suportado.", statusCode: StatusCodes.Status415UnsupportedMediaType);

        CriarRelatoRequest? entrada;
        try
        {
            entrada = await JsonSerializer.DeserializeAsync<CriarRelatoRequest>(
                httpContext.Request.Body,
                opcoesJson.Value.SerializerOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            return CorpoInvalido();
        }
        catch (NotSupportedException)
        {
            return CorpoInvalido();
        }

        var validacao = RelatoValidador.Validar(entrada, opcoesTriagem.Value.TamanhoMaximoRelato);
        if (!validacao.Valido)
            return Results.ValidationProblem(validacao.Erros, title: "Um ou mais campos são inválidos.");

        var chaveIdempotencia = httpContext.Request.Headers["Idempotency-Key"].ToString();
        if (chaveIdempotencia.Length > 100)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["Idempotency-Key"] = ["O cabeçalho Idempotency-Key deve ter no máximo 100 caracteres."]
                },
                title: "Um ou mais campos são inválidos.");
        }

        var resultado = await triagemService.ExecutarAsync(entrada!.Relato!.Trim(), chaveIdempotencia, cancellationToken);
        return MapearResultado(httpContext, resultado);
    }

    private static IResult MapearResultado(HttpContext httpContext, ResultadoTriagem resultado)
    {
        if (resultado.RetryAfterSegundos is { } retryAfter)
            httpContext.Response.Headers.RetryAfter = retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return resultado.Tipo switch
        {
            TipoResultadoTriagem.Gravada => Results.Ok(resultado.Ficha),
            TipoResultadoTriagem.Pergunta => Results.Json(resultado.Ficha, statusCode: StatusCodes.Status422UnprocessableEntity),
            TipoResultadoTriagem.SaidaLlmInvalida => CriarFalha(
                httpContext,
                StatusCodes.Status502BadGateway,
                "A IA não devolveu uma ficha válida.",
                "Foram feitas 3 tentativas sem saída válida. Nada foi gravado.",
                "saida_llm_invalida",
                resultado),
            TipoResultadoTriagem.IaIndisponivel => CriarFalha(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Serviço de triagem temporariamente indisponível.",
                "O serviço de IA está indisponível. Tente novamente mais tarde. Nada foi gravado.",
                "ia_indisponivel",
                resultado),
            TipoResultadoTriagem.BancoIndisponivel => CriarFalha(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Serviço de triagem temporariamente indisponível.",
                "O banco de dados está indisponível. Tente novamente mais tarde.",
                "banco_indisponivel",
                resultado),
            _ => throw new ArgumentOutOfRangeException(nameof(resultado))
        };
    }

    private static IResult CriarFalha(
        HttpContext httpContext,
        int status,
        string titulo,
        string detalhe,
        string codigo,
        ResultadoTriagem resultado) => Results.Problem(
            statusCode: status,
            title: titulo,
            detail: detalhe,
            extensions: new Dictionary<string, object?>
            {
                ["codigo"] = codigo,
                ["urgenciaRiscoDetectado"] = resultado.UrgenciaRiscoDetectado,
                ["idPedido"] = resultado.IdPedido,
                ["traceId"] = httpContext.TraceIdentifier
            });

    private static IResult CorpoInvalido() => Results.ValidationProblem(
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["body"] = ["O corpo deve conter JSON válido e somente o campo relato."]
        },
        title: "Um ou mais campos são inválidos.");
}
