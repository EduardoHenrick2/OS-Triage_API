using Microsoft.Extensions.Options;
using OsTriageApi.Relatos.Configuracao;
using OsTriageApi.Relatos.Dtos;
using OsTriageApi.Relatos.Modelos;
using OsTriageApi.Relatos.Validacao;

namespace OsTriageApi.Relatos.Aplicacao;

public sealed class TriagemService(
    ILlmClient llmClient,
    ITriagemRepository repository,
    IRelatoHasher relatoHasher,
    IOptions<TriagemOpcoes> triagemOpcoes,
    IOptions<GeminiOpcoes> geminiOpcoes)
{
    private readonly TriagemOpcoes _triagem = triagemOpcoes.Value;
    private readonly GeminiOpcoes _gemini = geminiOpcoes.Value;

    public async Task<ResultadoTriagem> ExecutarAsync(
        string relato,
        string? chaveIdempotencia,
        CancellationToken cancellationToken)
    {
        var idPedido = Guid.NewGuid();
        var chave = string.IsNullOrWhiteSpace(chaveIdempotencia)
            ? $"rest:{idPedido}"
            : chaveIdempotencia.Trim();
        var riscoDetectado = RegrasUrgencia.BuscarExpressao(relato) is not null;
        var relatoHash = relatoHasher.Calcular(relato);

        try
        {
            var existente = await repository.BuscarPorChaveAsync(chave, cancellationToken);
            if (existente is not null)
                return new ResultadoTriagem(TipoResultadoTriagem.Gravada, existente.IdPedido, existente);
        }
        catch (BancoIndisponivelException)
        {
            return new ResultadoTriagem(TipoResultadoTriagem.BancoIndisponivel, idPedido, UrgenciaRiscoDetectado: riscoDetectado, RetryAfterSegundos: 30);
        }

        RespostaLlm? respostaExtracao = null;
        FichaExtraida? ficha = null;
        short tentativaExtracaoValida = 0;

        for (short tentativa = 1; tentativa <= _triagem.MaximoTentativasExtracao; tentativa++)
        {
            try
            {
                respostaExtracao = await llmClient.ExtrairFichaAsync(relato, cancellationToken);
            }
            catch (LlmIndisponivelException exception)
            {
                await TentarRegistrarFalhaAsync(
                    CriarExecucao(idPedido, null, "extracao", tentativa, relatoHash, riscoDetectado, null, "falha_api", exception.CodigoErro),
                    cancellationToken);

                return new ResultadoTriagem(
                    TipoResultadoTriagem.IaIndisponivel,
                    idPedido,
                    UrgenciaRiscoDetectado: riscoDetectado,
                    RetryAfterSegundos: exception.RetryAfterSegundos);
            }

            var validacao = ValidadorSaidaLlm.Validar(respostaExtracao.Texto);
            if (validacao.Valido)
            {
                ficha = validacao.Ficha;
                tentativaExtracaoValida = tentativa;
                break;
            }

            try
            {
                await repository.RegistrarExecucaoAsync(
                    CriarExecucao(idPedido, null, "extracao", tentativa, relatoHash, riscoDetectado, respostaExtracao, "falha_formato", validacao.CodigoErro),
                    cancellationToken);
            }
            catch (BancoIndisponivelException)
            {
                return new ResultadoTriagem(TipoResultadoTriagem.BancoIndisponivel, idPedido, UrgenciaRiscoDetectado: riscoDetectado, RetryAfterSegundos: 30);
            }
        }

        if (ficha is null || respostaExtracao is null)
            return new ResultadoTriagem(TipoResultadoTriagem.SaidaLlmInvalida, idPedido, UrgenciaRiscoDetectado: riscoDetectado);

        var anoConfirmado = ConfirmadorAno.Confirmar(ficha.Ano, relato);
        var camposNaoConfirmados = ficha.Ano is not null && anoConfirmado is null
            ? new[] { "ano" }
            : Array.Empty<string>();
        ficha = ficha with { Ano = anoConfirmado };

        var regras = RegrasFicha.Avaliar(ficha, relato);
        if (!regras.Gravavel)
        {
            try
            {
                await repository.RegistrarExecucaoAsync(
                    CriarExecucao(idPedido, null, "extracao", tentativaExtracaoValida, relatoHash, riscoDetectado, respostaExtracao, "pergunta", null),
                    cancellationToken);
            }
            catch (BancoIndisponivelException)
            {
                return new ResultadoTriagem(TipoResultadoTriagem.BancoIndisponivel, idPedido, UrgenciaRiscoDetectado: riscoDetectado, RetryAfterSegundos: 30);
            }

            var respostaPergunta = CriarRespostaPergunta(idPedido, ficha, regras, camposNaoConfirmados);
            return new ResultadoTriagem(TipoResultadoTriagem.Pergunta, idPedido, respostaPergunta);
        }

        var sintomas = ficha.Sintomas!
            .Where(sintoma => !string.IsNullOrWhiteSpace(sintoma))
            .Select(sintoma => sintoma.Trim())
            .ToArray();
        var pecas = ficha.PecasProvaveis?.Where(peca => !string.IsNullOrWhiteSpace(peca)).Select(peca => peca.Trim()).ToArray()
            ?? [];
        var ordem = new OrdemParaGravar(
            idPedido,
            chave,
            ficha.ModeloMoto!,
            ficha.Ano,
            ficha.Categoria!.Value.ToCanonica(),
            ficha.Urgencia!.Value.ToValorApi(),
            regras.UrgenciaFinal!.Value.ToValorApi(),
            riscoDetectado,
            sintomas,
            pecas,
            camposNaoConfirmados);

        FichaServicoResponse gravada;
        try
        {
            gravada = await repository.GravarAsync(
                ordem,
                CriarExecucao(idPedido, null, "extracao", tentativaExtracaoValida, relatoHash, riscoDetectado, respostaExtracao, "gravada", null),
                cancellationToken);
        }
        catch (BancoIndisponivelException)
        {
            return new ResultadoTriagem(TipoResultadoTriagem.BancoIndisponivel, idPedido, UrgenciaRiscoDetectado: riscoDetectado, RetryAfterSegundos: 30);
        }

        var (mensagem, avisos) = await GerarMensagemAsync(gravada, relatoHash, riscoDetectado, cancellationToken);
        if (mensagem is not null)
        {
            try
            {
                await repository.AtualizarMensagemAsync(gravada.IdOrdem!.Value, mensagem, cancellationToken);
            }
            catch (BancoIndisponivelException)
            {
                return new ResultadoTriagem(TipoResultadoTriagem.BancoIndisponivel, idPedido, UrgenciaRiscoDetectado: riscoDetectado, RetryAfterSegundos: 30);
            }
        }

        gravada = gravada with { MensagemCliente = mensagem, Avisos = avisos };
        return new ResultadoTriagem(TipoResultadoTriagem.Gravada, gravada.IdPedido, gravada);
    }

    private async Task<(string? Mensagem, IReadOnlyList<string> Avisos)> GerarMensagemAsync(
        FichaServicoResponse ficha,
        string relatoHash,
        bool riscoDetectado,
        CancellationToken cancellationToken)
    {
        for (short tentativa = 1; tentativa <= _triagem.MaximoTentativasMensagem; tentativa++)
        {
            RespostaLlm resposta;
            try
            {
                resposta = await llmClient.GerarMensagemAsync(
                    new FichaParaMensagem(ficha.ModeloMoto!, ficha.Sintomas, ficha.Categoria!, ficha.UrgenciaFinal!),
                    cancellationToken);
            }
            catch (LlmIndisponivelException exception)
            {
                await TentarRegistrarFalhaAsync(
                    CriarExecucao(ficha.IdPedido, ficha.IdOrdem, "redacao_mensagem", tentativa, relatoHash, riscoDetectado, null, "falha_api", exception.CodigoErro),
                    cancellationToken);
                break;
            }

            if (LimitadorPalavras.DentroDoLimite(resposta.Texto, _triagem.LimiteMensagemPalavras))
            {
                await TentarRegistrarFalhaAsync(
                    CriarExecucao(ficha.IdPedido, ficha.IdOrdem, "redacao_mensagem", tentativa, relatoHash, riscoDetectado, resposta, "mensagem_gerada", null),
                    cancellationToken);
                return (resposta.Texto.Trim(), Array.Empty<string>());
            }

            await TentarRegistrarFalhaAsync(
                CriarExecucao(ficha.IdPedido, ficha.IdOrdem, "redacao_mensagem", tentativa, relatoHash, riscoDetectado, resposta, "falha_formato", "limite_palavras_excedido"),
                cancellationToken);
        }

        return (null, new[] { "mensagem_nao_gerada" });
    }

    private FichaServicoResponse CriarRespostaPergunta(
        Guid idPedido,
        FichaExtraida ficha,
        ResultadoRegrasFicha regras,
        IReadOnlyList<string> camposNaoConfirmados)
    {
        var pergunta = regras.CamposFaltando.Contains("modeloMoto", StringComparer.Ordinal)
            ? "Qual é o modelo da moto?"
            : "Qual é o problema que o cliente descreve?";

        return new FichaServicoResponse(
            idPedido,
            null,
            "pergunta",
            ficha.ModeloMoto,
            ficha.Ano,
            ficha.Sintomas ?? [],
            ficha.Categoria?.ToCanonica(),
            ficha.Urgencia?.ToValorApi(),
            regras.UrgenciaFinal?.ToValorApi(),
            ficha.PecasProvaveis ?? [],
            regras.UrgenciaFinal == UrgenciaRelato.Alta,
            regras.CamposFaltando,
            pergunta,
            camposNaoConfirmados,
            null,
            []);
    }

    private ExecucaoLlm CriarExecucao(
        Guid idPedido,
        Guid? ordemId,
        string etapa,
        short tentativa,
        string relatoHash,
        bool riscoDetectado,
        RespostaLlm? resposta,
        string resultado,
        string? codigoErro) => new(
            Guid.NewGuid(),
            idPedido,
            ordemId,
            etapa,
            tentativa,
            _gemini.VersaoPrompt,
            resposta?.Modelo ?? _gemini.Modelo,
            resposta?.Temperatura ?? _gemini.Temperatura,
            resposta?.TempoMs ?? 0,
            resposta?.TokensEntrada,
            resposta?.TokensSaida,
            relatoHash,
            resposta?.Texto,
            riscoDetectado,
            resultado,
            codigoErro);

    private async Task TentarRegistrarFalhaAsync(ExecucaoLlm execucao, CancellationToken cancellationToken)
    {
        try
        {
            await repository.RegistrarExecucaoAsync(execucao, cancellationToken);
        }
        catch (BancoIndisponivelException)
        {
            // A falha principal continua sendo devolvida; o banco indisponível impediu apenas a auditoria.
        }
    }
}
