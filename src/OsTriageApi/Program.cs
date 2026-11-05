using OsTriageApi.Endpoints;
using OsTriageApi.Relatos.Configuracao;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services
    .AddOptions<TriagemOpcoes>()
    .BindConfiguration(TriagemOpcoes.Secao)
    .Validate(opcoes => opcoes.TamanhoMaximoRelato > 0, "Triagem:TamanhoMaximoRelato deve ser maior que zero.")
    .ValidateOnStart();

builder.Services.ConfigureHttpJsonOptions(opcoes =>
    OpcoesJsonRelato.Configurar(opcoes.SerializerOptions));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapTriagemEndpoints();

app.Run();

public partial class Program;
