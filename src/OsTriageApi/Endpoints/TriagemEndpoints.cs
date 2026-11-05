using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
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
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status501NotImplemented);
        return endpoints;
    }

    private static async Task<IResult> ProcessarAsync(
        HttpRequest request,
        IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> opcoesJson,
        IOptions<TriagemOpcoes> opcoesTriagem,
        CancellationToken cancellationToken)
    {
        CriarRelatoRequest? entrada;

        try
        {
            entrada = await JsonSerializer.DeserializeAsync<CriarRelatoRequest>(
                request.Body,
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

        return Results.Problem(
            title: "Triagem ainda não implementada.",
            detail: "O contrato foi validado, mas o caso de uso UC01 ainda não foi implementado.",
            statusCode: StatusCodes.Status501NotImplemented);
    }

    private static IResult CorpoInvalido() => Results.ValidationProblem(
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["body"] = ["O corpo deve conter JSON válido e somente o campo relato."]
        },
        title: "Um ou mais campos são inválidos.");
}
