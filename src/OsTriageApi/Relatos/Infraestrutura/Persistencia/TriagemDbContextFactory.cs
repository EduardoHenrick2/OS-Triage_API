using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OsTriageApi.Relatos.Infraestrutura.Persistencia;

public sealed class TriagemDbContextFactory : IDesignTimeDbContextFactory<TriagemDbContext>
{
    public TriagemDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=127.0.0.1;Database=os_triage_api;Username=os_triage_app";
        var options = new DbContextOptionsBuilder<TriagemDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new TriagemDbContext(options);
    }
}
