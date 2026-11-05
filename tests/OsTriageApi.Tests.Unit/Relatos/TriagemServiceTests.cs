using Microsoft.Extensions.Options;
using OsTriageApi.Relatos.Aplicacao;
using OsTriageApi.Relatos.Configuracao;
using OsTriageApi.Relatos.Dtos;

namespace OsTriageApi.Tests.Unit.Relatos;

public sealed class TriagemServiceTests
{
    [Fact]
    public async Task Executar_FichaCompleta_GravaEDevolveSucesso()
    {
        var contexto = CriarContexto(SaidaValida());

        var resultado = await contexto.Service.ExecutarAsync(
            "Yamaha Fazer 250 2019. A corrente faz ruído na aceleração.",
            "pedido-001",
            CancellationToken.None);

        Assert.Equal(TipoResultadoTriagem.Gravada, resultado.Tipo);
        Assert.Equal(1, contexto.Repository.QuantidadeGravacoes);
        Assert.Equal("corrente e relação", resultado.Ficha?.Categoria);
        Assert.Equal("media", resultado.Ficha?.UrgenciaFinal);
    }

    [Fact]
    public async Task Executar_SemModelo_RetornaPerguntaESemGravacao()
    {
        var contexto = CriarContexto(SaidaValida(modelo: null));

        var resultado = await contexto.Service.ExecutarAsync(
            "Está fazendo barulho no motor e perdeu força. Ano 2017.",
            "pedido-002",
            CancellationToken.None);

        Assert.Equal(TipoResultadoTriagem.Pergunta, resultado.Tipo);
        Assert.Equal(0, contexto.Repository.QuantidadeGravacoes);
        Assert.Equal(new[] { "modeloMoto" }, resultado.Ficha?.CamposFaltando);
    }

    [Fact]
    public async Task Executar_SaidaInvalidaTresVezes_RetornaBadGatewayESemGravacao()
    {
        var contexto = CriarContexto("{}", "{}", "{}");

        var resultado = await contexto.Service.ExecutarAsync(
            "Honda CG 160 com ruído no motor.",
            "pedido-003",
            CancellationToken.None);

        Assert.Equal(TipoResultadoTriagem.SaidaLlmInvalida, resultado.Tipo);
        Assert.Equal(3, contexto.Llm.QuantidadeExtracoes);
        Assert.Equal(3, contexto.Repository.Logs.Count);
        Assert.Equal(0, contexto.Repository.QuantidadeGravacoes);
    }

    [Fact]
    public async Task Executar_GeminiIndisponivel_RetornaServiceUnavailableComRetryAfter()
    {
        var llm = new LlmFake(new LlmIndisponivelException("http_429", 30));
        var contexto = CriarContexto(llm);

        var resultado = await contexto.Service.ExecutarAsync(
            "Honda CG 160 com ruído no motor.",
            "pedido-004",
            CancellationToken.None);

        Assert.Equal(TipoResultadoTriagem.IaIndisponivel, resultado.Tipo);
        Assert.Equal(30, resultado.RetryAfterSegundos);
        Assert.Equal(0, contexto.Repository.QuantidadeGravacoes);
    }

    [Fact]
    public async Task Executar_MesmaChave_DevolveMesmaFichaSemNovaExtracao()
    {
        var contexto = CriarContexto(SaidaValida());
        var primeiro = await contexto.Service.ExecutarAsync("Yamaha Fazer 250 2019 com ruído na corrente.", "mesma-chave", CancellationToken.None);
        var segundo = await contexto.Service.ExecutarAsync("Texto repetido.", "mesma-chave", CancellationToken.None);

        Assert.Equal(primeiro.Ficha?.IdOrdem, segundo.Ficha?.IdOrdem);
        Assert.Equal(primeiro.Ficha?.IdPedido, segundo.Ficha?.IdPedido);
        Assert.Equal(1, contexto.Llm.QuantidadeExtracoes);
        Assert.Equal(1, contexto.Repository.QuantidadeGravacoes);
    }

    [Fact]
    public async Task Executar_RiscoComUrgenciaBaixa_ElevaParaAlta()
    {
        var contexto = CriarContexto(SaidaValida(urgencia: "baixa", categoria: "freios", sintomas: new[] { "freio fraco" }));

        var resultado = await contexto.Service.ExecutarAsync(
            "Yamaha Fazer 250 2019 com freio fraco.",
            "pedido-005",
            CancellationToken.None);

        Assert.Equal("baixa", resultado.Ficha?.UrgenciaLlm);
        Assert.Equal("alta", resultado.Ficha?.UrgenciaFinal);
        Assert.True(resultado.Ficha?.AvisoUrgencia);
    }

    [Fact]
    public async Task Executar_AnoQueNaoApareceNoRelato_RemoveAno()
    {
        var contexto = CriarContexto(SaidaValida(ano: 2020));

        var resultado = await contexto.Service.ExecutarAsync(
            "Yamaha Fazer 250 com ruído na corrente.",
            "pedido-006",
            CancellationToken.None);

        Assert.Null(resultado.Ficha?.Ano);
        Assert.Equal(new[] { "ano" }, resultado.Ficha?.CamposNaoConfirmados);
    }

    [Fact]
    public async Task Executar_MensagemAcimaDoLimite_TentaNovamenteAteFicarValida()
    {
        var contexto = CriarContexto(SaidaValida());
        contexto.Llm.Mensagens.Clear();
        contexto.Llm.Mensagens.Enqueue(string.Join(' ', Enumerable.Repeat("palavra", 61)));
        contexto.Llm.Mensagens.Enqueue(string.Join(' ', Enumerable.Repeat("palavra", 61)));
        contexto.Llm.Mensagens.Enqueue("Mensagem final dentro do limite definido.");

        var resultado = await contexto.Service.ExecutarAsync(
            "Yamaha Fazer 250 2019 com ruído na corrente.",
            "pedido-007",
            CancellationToken.None);

        Assert.Equal(3, contexto.Llm.QuantidadeMensagens);
        Assert.Equal("Mensagem final dentro do limite definido.", resultado.Ficha?.MensagemCliente);
    }

    private static ContextoTeste CriarContexto(params string[] extracoes) =>
        CriarContexto(new LlmFake(extracoes));

    private static ContextoTeste CriarContexto(LlmFake llm)
    {
        var repository = new RepositoryFake();
        var triagem = Options.Create(new TriagemOpcoes
        {
            TamanhoMaximoRelato = 2_000,
            MaximoTentativasExtracao = 3,
            MaximoTentativasMensagem = 3,
            LimiteMensagemPalavras = 60
        });
        var gemini = Options.Create(new GeminiOpcoes
        {
            Modelo = "modelo-simulado",
            VersaoPrompt = "teste",
            Temperatura = 0.2m,
            TimeoutSegundos = 10
        });
        var service = new TriagemService(llm, repository, new HasherFake(), triagem, gemini);
        return new ContextoTeste(service, llm, repository);
    }

    private static string SaidaValida(
        string? modelo = "Yamaha Fazer 250",
        int? ano = 2019,
        string categoria = "corrente e relação",
        string urgencia = "media",
        string[]? sintomas = null)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            modeloMoto = modelo,
            ano,
            sintomas = sintomas ?? new[] { "ruído na corrente" },
            categoria,
            urgencia,
            pecasProvaveis = new[] { "corrente" }
        });
    }

    private sealed record ContextoTeste(TriagemService Service, LlmFake Llm, RepositoryFake Repository);

    private sealed class HasherFake : IRelatoHasher
    {
        public string Calcular(string relato) => new('a', 64);
    }

    private sealed class LlmFake : ILlmClient
    {
        private readonly Queue<object> _extracoes;

        public LlmFake(params string[] extracoes) => _extracoes = new Queue<object>(extracoes.Cast<object>());
        public LlmFake(Exception exception) => _extracoes = new Queue<object>(new object[] { exception });

        public int QuantidadeExtracoes { get; private set; }
        public int QuantidadeMensagens { get; private set; }
        public Queue<string> Mensagens { get; } = new(["Recebemos sua moto e faremos a avaliação do serviço informado."]);

        public Task<RespostaLlm> ExtrairFichaAsync(string relato, CancellationToken cancellationToken)
        {
            QuantidadeExtracoes++;
            var proximo = _extracoes.Dequeue();
            if (proximo is Exception exception)
                return Task.FromException<RespostaLlm>(exception);
            return Task.FromResult(new RespostaLlm((string)proximo, "modelo-simulado", 0.2m, 10, 10, 20));
        }

        public Task<RespostaLlm> GerarMensagemAsync(FichaParaMensagem ficha, CancellationToken cancellationToken)
        {
            QuantidadeMensagens++;
            return Task.FromResult(new RespostaLlm(Mensagens.Dequeue(), "modelo-simulado", 0.2m, 5, 10, 15));
        }
    }

    private sealed class RepositoryFake : ITriagemRepository
    {
        private readonly Dictionary<string, FichaServicoResponse> _ordens = new(StringComparer.Ordinal);

        public int QuantidadeGravacoes { get; private set; }
        public List<ExecucaoLlm> Logs { get; } = [];

        public Task<FichaServicoResponse?> BuscarPorChaveAsync(string chaveIdempotencia, CancellationToken cancellationToken)
        {
            _ordens.TryGetValue(chaveIdempotencia, out var ficha);
            return Task.FromResult(ficha);
        }

        public Task<FichaServicoResponse> GravarAsync(OrdemParaGravar ordem, ExecucaoLlm execucao, CancellationToken cancellationToken)
        {
            if (_ordens.TryGetValue(ordem.ChaveIdempotencia, out var existente))
                return Task.FromResult(existente);

            QuantidadeGravacoes++;
            var ficha = new FichaServicoResponse(
                ordem.IdPedido,
                Guid.NewGuid(),
                "gravada",
                ordem.ModeloMoto,
                ordem.Ano,
                ordem.Sintomas,
                ordem.Categoria,
                ordem.UrgenciaLlm,
                ordem.UrgenciaFinal,
                ordem.PecasProvaveis,
                ordem.UrgenciaFinal == "alta",
                [],
                null,
                ordem.CamposNaoConfirmados,
                null,
                []);
            _ordens[ordem.ChaveIdempotencia] = ficha;
            Logs.Add(execucao);
            return Task.FromResult(ficha);
        }

        public Task RegistrarExecucaoAsync(ExecucaoLlm execucao, CancellationToken cancellationToken)
        {
            Logs.Add(execucao);
            return Task.CompletedTask;
        }

        public Task AtualizarMensagemAsync(Guid ordemId, string? mensagem, CancellationToken cancellationToken)
        {
            var item = _ordens.Single(par => par.Value.IdOrdem == ordemId);
            _ordens[item.Key] = item.Value with { MensagemCliente = mensagem };
            return Task.CompletedTask;
        }
    }
}
