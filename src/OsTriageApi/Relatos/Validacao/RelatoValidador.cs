using OsTriageApi.Relatos.Dtos;

namespace OsTriageApi.Relatos.Validacao;

public static class RelatoValidador
{
    public static ResultadoValidacao Validar(CriarRelatoRequest? request, int tamanhoMaximoRelato)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Relato))
            return Invalido("O campo relato é obrigatório e não pode ficar vazio.");

        if (request.Relato.Length > tamanhoMaximoRelato)
            return Invalido($"O campo relato deve ter no máximo {tamanhoMaximoRelato} caracteres.");

        return new ResultadoValidacao(true, new Dictionary<string, string[]>());
    }

    private static ResultadoValidacao Invalido(string mensagem) =>
        new(false, new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["relato"] = [mensagem]
        });
}
