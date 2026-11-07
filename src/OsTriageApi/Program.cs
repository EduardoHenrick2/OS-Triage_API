using Microsoft.EntityFrameworkCore;
using Npgsql;
using OsTriageApi.Endpoints;
using OsTriageApi.Relatos.Aplicacao;
using OsTriageApi.Relatos.Configuracao;
using OsTriageApi.Relatos.Infraestrutura;
using OsTriageApi.Relatos.Infraestrutura.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services
    .AddOptions<TriagemOpcoes>()
    .BindConfiguration(TriagemOpcoes.Secao)
    .Validate(opcoes => opcoes.TamanhoMaximoRelato > 0, "Triagem:TamanhoMaximoRelato deve ser maior que zero.")
    .Validate(opcoes => opcoes.MaximoTentativasExtracao is >= 1 and <= 3, "Triagem:MaximoTentativasExtracao deve estar entre 1 e 3.")
    .Validate(opcoes => opcoes.MaximoTentativasMensagem is >= 1 and <= 3, "Triagem:MaximoTentativasMensagem deve estar entre 1 e 3.")
    .Validate(opcoes => opcoes.LimiteMensagemPalavras > 0, "Triagem:LimiteMensagemPalavras deve ser maior que zero.")
    .ValidateOnStart();
builder.Services
    .AddOptions<GeminiOpcoes>()
    .BindConfiguration(GeminiOpcoes.Secao)
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.Modelo), "Gemini:Modelo é obrigatório.")
    .Validate(opcoes => !string.IsNullOrWhiteSpace(opcoes.ApiKey), "Gemini:ApiKey deve ser configurada em user-secrets.")
    .Validate(opcoes => opcoes.Temperatura == 0.2m, "Gemini:Temperatura deve ser 0.2.")
    .Validate(opcoes => opcoes.TimeoutSegundos == 10, "Gemini:TimeoutSegundos deve ser 10.")
    .ValidateOnStart();
builder.Services
    .AddOptions<PrivacidadeOpcoes>()
    .BindConfiguration(PrivacidadeOpcoes.Secao)
    .Validate(opcoes => opcoes.ChaveHmac.Length >= 32, "Privacidade:ChaveHmac deve ter pelo menos 32 caracteres e ficar em user-secrets.")
    .ValidateOnStart();

builder.Services.ConfigureHttpJsonOptions(opcoes =>
    OpcoesJsonRelato.Configurar(opcoes.SerializerOptions));

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres deve ser configurada em user-secrets ou pelo ambiente do contêiner.");
var postgresPassword = builder.Configuration["PostgresPassword"];
if (!string.IsNullOrWhiteSpace(postgresPassword))
{
    var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Password = postgresPassword
    };
    connectionString = connectionBuilder.ConnectionString;
}
builder.Services.AddDbContext<TriagemDbContext>(opcoes => opcoes.UseNpgsql(connectionString));
builder.Services.AddScoped<ITriagemRepository, EfTriagemRepository>();
builder.Services.AddScoped<IRelatoHasher, RelatoHasher>();
builder.Services.AddScoped<TriagemService>();
builder.Services.AddHttpClient<ILlmClient, GeminiLlmClient>((services, client) =>
{
    var opcoes = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeminiOpcoes>>().Value;
    client.BaseAddress = new Uri(opcoes.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opcoes.TimeoutSegundos);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TriagemDbContext>();
    dbContext.Database.Migrate();
    app.MapOpenApi();
}

app.MapTriagemEndpoints();

app.Run();

public partial class Program;
