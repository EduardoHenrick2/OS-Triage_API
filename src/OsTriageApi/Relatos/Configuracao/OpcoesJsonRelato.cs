using System.Text.Json;
using System.Text.Json.Serialization;

namespace OsTriageApi.Relatos.Configuracao;

public static class OpcoesJsonRelato
{
    public static void Configurar(JsonSerializerOptions opcoes)
    {
        opcoes.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    }
}
