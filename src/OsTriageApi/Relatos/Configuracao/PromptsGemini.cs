using System.Text.RegularExpressions;

namespace OsTriageApi.Relatos.Configuracao;

// Prompts do Gemini carregados de prompts/{versao}.md. O texto dos blocos é enviado sem alterações.
public sealed class PromptsGemini
{
    public const string DiretorioPadrao = "prompts";

    private const string SecaoExtracao = "extracao";
    private const string SecaoRedacao = "redacao";

    private static readonly Regex VersaoValida = new(
        @"^[A-Za-z0-9][A-Za-z0-9._-]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex Marcador = new(
        @"\{\{([a-z]+)\}\}",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] MarcadoresExtracao = ["relato"];
    private static readonly string[] MarcadoresRedacao = ["modelo", "sintomas", "categoria", "urgencia"];

    private readonly string _extracao;
    private readonly string _redacao;

    private PromptsGemini(string versao, string caminho, string extracao, string redacao)
    {
        Versao = versao;
        Caminho = caminho;
        _extracao = extracao;
        _redacao = redacao;
    }

    public string Versao { get; }

    public string Caminho { get; }

    public static string CaminhoEsperado(string diretorioBase, string versao)
    {
        ValidarVersao(versao);
        return Path.GetFullPath(Path.Combine(diretorioBase, DiretorioPadrao, $"{versao}.md"));
    }

    public static PromptsGemini Carregar(string diretorioBase, string versao)
    {
        var caminho = CaminhoEsperado(diretorioBase, versao);
        if (!File.Exists(caminho))
        {
            throw new InvalidOperationException(
                $"O arquivo de prompt da versão '{versao}' não foi encontrado. Caminho esperado: {caminho}. " +
                "Confira Gemini:VersaoPrompt e a pasta prompts/ do repositório.");
        }

        var linhas = File.ReadAllText(caminho).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var secoes = LerSecoes(linhas, caminho);

        var extracao = ObterSecao(secoes, SecaoExtracao, caminho);
        var redacao = ObterSecao(secoes, SecaoRedacao, caminho);
        ValidarMarcadores(extracao, MarcadoresExtracao, SecaoExtracao, caminho);
        ValidarMarcadores(redacao, MarcadoresRedacao, SecaoRedacao, caminho);

        return new PromptsGemini(versao, caminho, extracao, redacao);
    }

    public string RenderizarExtracao(string relato) =>
        Renderizar(_extracao, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["relato"] = relato
        });

    public string RenderizarRedacao(string modelo, string sintomas, string categoria, string urgencia) =>
        Renderizar(_redacao, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["modelo"] = modelo,
            ["sintomas"] = sintomas,
            ["categoria"] = categoria,
            ["urgencia"] = urgencia
        });

    private static string Renderizar(string modelo, IReadOnlyDictionary<string, string> valores) =>
        Marcador.Replace(modelo, correspondencia => valores[correspondencia.Groups[1].Value]);

    private static void ValidarVersao(string versao)
    {
        if (string.IsNullOrWhiteSpace(versao) || !VersaoValida.IsMatch(versao))
        {
            throw new InvalidOperationException(
                $"Gemini:VersaoPrompt inválida: '{versao}'. Use apenas letras, números, '.', '_' e '-', como 'v1'.");
        }
    }

    private static Dictionary<string, string> LerSecoes(string[] linhas, string caminho)
    {
        var secoes = new Dictionary<string, string>(StringComparer.Ordinal);
        string? secaoAtual = null;

        for (var indice = 0; indice < linhas.Length; indice++)
        {
            var linha = linhas[indice].TrimEnd();

            if (linha.StartsWith("## ", StringComparison.Ordinal))
            {
                secaoAtual = linha[3..].Trim();
                continue;
            }

            if (secaoAtual is null || linha != "```text")
                continue;

            var conteudo = new List<string>();
            var fechado = false;
            for (indice++; indice < linhas.Length; indice++)
            {
                if (linhas[indice].TrimEnd() == "```")
                {
                    fechado = true;
                    break;
                }

                conteudo.Add(linhas[indice]);
            }

            if (!fechado)
                throw new InvalidOperationException($"O bloco da seção '{secaoAtual}' não foi fechado em {caminho}.");

            if (!secoes.TryAdd(secaoAtual, string.Join('\n', conteudo)))
                throw new InvalidOperationException($"A seção '{secaoAtual}' tem mais de um bloco em {caminho}.");
        }

        return secoes;
    }

    private static string ObterSecao(Dictionary<string, string> secoes, string nome, string caminho)
    {
        if (!secoes.TryGetValue(nome, out var texto) || string.IsNullOrWhiteSpace(texto))
            throw new InvalidOperationException($"A seção '## {nome}' com bloco 'text' está ausente ou vazia em {caminho}.");

        return texto;
    }

    private static void ValidarMarcadores(string texto, string[] obrigatorios, string secao, string caminho)
    {
        var encontrados = Marcador.Matches(texto).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        var desconhecido = encontrados.FirstOrDefault(nome => !obrigatorios.Contains(nome, StringComparer.Ordinal));
        if (desconhecido is not null)
            throw new InvalidOperationException($"A seção '{secao}' usa o marcador desconhecido '{{{{{desconhecido}}}}}' em {caminho}.");

        var ausente = obrigatorios.FirstOrDefault(nome => !encontrados.Contains(nome));
        if (ausente is not null)
            throw new InvalidOperationException($"A seção '{secao}' não contém o marcador obrigatório '{{{{{ausente}}}}}' em {caminho}.");
    }
}
