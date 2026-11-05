using Microsoft.EntityFrameworkCore;
using Npgsql;
using OsTriageApi.Relatos.Aplicacao;
using OsTriageApi.Relatos.Dtos;

namespace OsTriageApi.Relatos.Infraestrutura.Persistencia;

public sealed class EfTriagemRepository(TriagemDbContext dbContext) : ITriagemRepository
{
    public async Task<FichaServicoResponse?> BuscarPorChaveAsync(
        string chaveIdempotencia,
        CancellationToken cancellationToken)
    {
        try
        {
            var ordem = await dbContext.OrdensServico
                .AsNoTracking()
                .Include(x => x.Sintomas)
                .SingleOrDefaultAsync(x => x.ChaveIdempotencia == chaveIdempotencia, cancellationToken);
            if (ordem is null)
                return null;

            var idPedido = await dbContext.LogsExecucoes
                .AsNoTracking()
                .Where(x => x.OrdemId == ordem.Id && x.Resultado == "gravada")
                .Select(x => x.IdPedido)
                .FirstAsync(cancellationToken);

            return Mapear(ordem, idPedido);
        }
        catch (Exception exception) when (EhFalhaBanco(exception))
        {
            throw new BancoIndisponivelException(exception);
        }
    }

    public async Task<FichaServicoResponse> GravarAsync(
        OrdemParaGravar ordem,
        ExecucaoLlm execucao,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transacao = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var entidade = CriarOrdem(ordem);
                dbContext.OrdensServico.Add(entidade);
                dbContext.LogsExecucoes.Add(CriarExecucao(execucao with { OrdemId = entidade.Id }));
                await dbContext.SaveChangesAsync(cancellationToken);
                await transacao.CommitAsync(cancellationToken);
                return Mapear(entidade, ordem.IdPedido);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transacao.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                return await BuscarPorChaveAsync(ordem.ChaveIdempotencia, cancellationToken)
                    ?? throw new BancoIndisponivelException(exception);
            }
            catch
            {
                await transacao.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (BancoIndisponivelException)
        {
            throw;
        }
        catch (Exception exception) when (EhFalhaBanco(exception))
        {
            throw new BancoIndisponivelException(exception);
        }
    }

    public async Task RegistrarExecucaoAsync(ExecucaoLlm execucao, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.LogsExecucoes.Add(CriarExecucao(execucao));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (EhFalhaBanco(exception))
        {
            throw new BancoIndisponivelException(exception);
        }
    }

    public async Task AtualizarMensagemAsync(Guid ordemId, string? mensagem, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.OrdensServico
                .Where(x => x.Id == ordemId)
                .ExecuteUpdateAsync(atualizacao => atualizacao.SetProperty(x => x.MensagemCliente, mensagem), cancellationToken);
        }
        catch (Exception exception) when (EhFalhaBanco(exception))
        {
            throw new BancoIndisponivelException(exception);
        }
    }

    private static OrdemServicoEntity CriarOrdem(OrdemParaGravar ordem)
    {
        var entidade = new OrdemServicoEntity
        {
            Id = Guid.NewGuid(),
            ChaveIdempotencia = ordem.ChaveIdempotencia,
            ModeloMoto = ordem.ModeloMoto,
            Ano = ordem.Ano,
            Categoria = ordem.Categoria,
            UrgenciaLlm = ordem.UrgenciaLlm,
            UrgenciaFinal = ordem.UrgenciaFinal,
            RiscoDetectado = ordem.RiscoDetectado,
            PecasProvaveis = ordem.PecasProvaveis.ToArray(),
            CamposNaoConfirmados = ordem.CamposNaoConfirmados.ToArray(),
            DataCriacao = DateTimeOffset.UtcNow
        };

        entidade.Sintomas = ordem.Sintomas.Select((descricao, indice) => new SintomaEntity
        {
            OrdemId = entidade.Id,
            Posicao = checked((short)(indice + 1)),
            Descricao = descricao
        }).ToList();
        return entidade;
    }

    private static ExecucaoAuditoriaEntity CriarExecucao(ExecucaoLlm execucao) => new()
    {
        IdExecucao = execucao.IdExecucao,
        IdPedido = execucao.IdPedido,
        OrdemId = execucao.OrdemId,
        Etapa = execucao.Etapa,
        Tentativa = execucao.Tentativa,
        DataHora = DateTimeOffset.UtcNow,
        VersaoPrompt = execucao.VersaoPrompt,
        ModeloLlm = execucao.ModeloLlm,
        Temperatura = execucao.Temperatura,
        TempoMs = execucao.TempoMs,
        TokensEntrada = execucao.TokensEntrada,
        TokensSaida = execucao.TokensSaida,
        RelatoHash = execucao.RelatoHash,
        RespostaBruta = execucao.RespostaBruta,
        RiscoDetectado = execucao.RiscoDetectado,
        Resultado = execucao.Resultado,
        CodigoErro = execucao.CodigoErro
    };

    private static FichaServicoResponse Mapear(OrdemServicoEntity ordem, Guid idPedido) => new(
        idPedido,
        ordem.Id,
        "gravada",
        ordem.ModeloMoto,
        ordem.Ano,
        ordem.Sintomas.OrderBy(x => x.Posicao).Select(x => x.Descricao).ToArray(),
        ordem.Categoria,
        ordem.UrgenciaLlm,
        ordem.UrgenciaFinal,
        ordem.PecasProvaveis,
        ordem.UrgenciaFinal == "alta",
        [],
        null,
        ordem.CamposNaoConfirmados,
        ordem.MensagemCliente,
        []);

    private static bool EhFalhaBanco(Exception exception) =>
        exception is NpgsqlException or DbUpdateException or InvalidOperationException;
}
