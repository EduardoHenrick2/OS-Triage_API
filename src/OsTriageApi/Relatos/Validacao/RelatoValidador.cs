using OsTriageApi.Relatos.Dtos;
using OsTriageApi.Relatos.Modelos;

namespace OsTriageApi.Relatos.Validacao;

// Validação determinística: mesma entrada, mesmo resultado. Não usa LLM nem reflexão.
public static class RelatoValidador
{
    public const int DescricaoMin = 10;
    public const int DescricaoMax = 2000;
    public const int DetalheMax = 100;
    public const int ModeloMax = 100;

    private const string CategoriaOutro = "outro ou não identificado";

    public static ResultadoValidacao Validar(CriarRelatoRequest request)
    {
        var erros = new List<string>();

        // 1. Campos obrigatórios e limites de tamanho
        var descricao = request.Descricao?.Trim();
        if (string.IsNullOrEmpty(descricao))
            erros.Add("Descricao é obrigatória.");
        else if (descricao.Length < DescricaoMin || descricao.Length > DescricaoMax)
            erros.Add($"Descricao deve ter entre {DescricaoMin} e {DescricaoMax} caracteres.");

        // 2. Categoria: exatamente uma das 8 canônicas
        CategoriaRelato? categoria = null;
        var categoriaTexto = request.Categoria?.Trim();
        if (string.IsNullOrEmpty(categoriaTexto))
            erros.Add("Categoria é obrigatória.");
        else if (CategoriasCanonicas.TryObter(categoriaTexto, out var encontrada))
            categoria = encontrada;
        else
            erros.Add($"Categoria inválida. Valores aceitos: {string.Join("; ", CategoriasCanonicas.Valores)}.");

        // 3. Regras de categoria e demais campos opcionais
        var detalhe = NormalizarOpcional(request.DetalheCategoria);
        if (categoriaTexto == CategoriaOutro && detalhe is null)
            erros.Add($"DetalheCategoria é obrigatório quando a categoria é '{CategoriaOutro}'.");
        if (detalhe is { Length: > DetalheMax })
            erros.Add($"DetalheCategoria deve ter no máximo {DetalheMax} caracteres.");

        var modelo = NormalizarOpcional(request.ModeloMoto);
        if (modelo is { Length: > ModeloMax })
            erros.Add($"ModeloMoto deve ter no máximo {ModeloMax} caracteres.");

        // 4. Piso de urgência: roda mesmo com erros de campo
        var expressao = string.IsNullOrEmpty(descricao)
            ? null
            : RegrasUrgencia.BuscarExpressao(descricao);

        var urgenciaMinima = expressao is null ? UrgenciaRelato.Baixa : UrgenciaRelato.Alta;

        if (erros.Count > 0 || categoria is null || descricao is null)
            return new ResultadoValidacao(false, erros, urgenciaMinima, expressao, null);

        var relato = new Relato(descricao, categoria.Value, detalhe, modelo, urgenciaMinima, expressao);
        return new ResultadoValidacao(true, [], urgenciaMinima, expressao, relato);
    }

    private static string? NormalizarOpcional(string? valor)
    {
        var limpo = valor?.Trim();
        return string.IsNullOrEmpty(limpo) ? null : limpo;
    }
}
