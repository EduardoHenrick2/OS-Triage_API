using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OsTriageApi.Relatos.Aplicacao;
using OsTriageApi.Relatos.Configuracao;

namespace OsTriageApi.Relatos.Infraestrutura;

public sealed class RelatoHasher(IOptions<PrivacidadeOpcoes> opcoes) : IRelatoHasher
{
    private readonly byte[] _chave = Encoding.UTF8.GetBytes(opcoes.Value.ChaveHmac);

    public string Calcular(string relato)
    {
        using var hmac = new HMACSHA256(_chave);
        return Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(relato)));
    }
}
