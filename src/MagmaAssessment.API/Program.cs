using MagmaAssessment.Core.Interfaces;
using MagmaAssessment.Infrastructure.Services;
using MagmaAssessment.Infrastructure.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/magma-assessment-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Magma3 Assessment API",
        Version = "v1",
        Description = "API desenvolvida para o desafio técnico Magma3 - Backend Developer\n\n" +
                      "**Questões Implementadas:**\n" +
                      "- Questão 1: Consumo da API Force1 com Polly (Retry + Circuit Breaker)\n" +
                      "- Questão 2: API REST de Produtos (CRUD completo)\n" +
                      "- Questão 3: Integração MongoDB com Repository Pattern\n" +
                      "- Questão 4: Correção de código (implementado)\n" +
                      "- Questão 5: Integrações com Google Maps, DocuSign e Microsoft Graph",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Bruno Henrique",
            Email = "brunoricksp@gmail.com",
            Url = new Uri("https://github.com/brunohenss")
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddHttpClient<IForce1Service, Force1Service>();

builder.Services.AddSingleton<IProdutoRepository, ProdutoRepository>();

builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();
builder.Services.AddScoped<IDocuSignService, DocuSignService>();
builder.Services.AddScoped<IMicrosoftGraphService, MicrosoftGraphService>();

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Magma3 Assessment API v1");
        options.RoutePrefix = string.Empty; // Swagger na raiz (http://localhost:5059)
        options.DocumentTitle = "Magma3 Assessment API";
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    service = "Magma3 Backend Assessment",
    environment = app.Environment.EnvironmentName,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("Health")
.WithOpenApi();

app.MapGet("/ativos", async (IForce1Service force1Service) =>
{
    var ativos = await force1Service.ObterTodosAtivos();
    return Results.Ok(new
    {
        total = ativos.Count,
        ativos = ativos
    });
})
.WithName("GetAtivos")
.WithTags("Force1")
.WithOpenApi();

app.MapGet("/computadores-inativos", async (IForce1Service force1Service) =>
{
    var computadoresInativos = await force1Service.ObterComputadoresInativos();
    return Results.Ok(new
    {
        total = computadoresInativos.Count,
        computadores = computadoresInativos,
        criterio = "Mais de 60 dias sem comunicação"
    });
})
.WithName("GetComputadoresInativos")
.WithTags("Force1")
.WithOpenApi();

app.MapGet("/ativo/{id}", async (string id, IForce1Service force1Service) =>
{
    var ativo = await force1Service.ObterAtivoPorId(id);
    return ativo is not null ? Results.Ok(ativo) : Results.NotFound(new { mensagem = $"Ativo com ID {id} não encontrado" });
})
.WithName("GetAtivoPorId")
.WithTags("Force1")
.WithOpenApi();

try
{
    Log.Information("===========================================");
    Log.Information("Iniciando Magma3 Backend Assessment API");
    Log.Information("Ambiente: {Environment}", app.Environment.EnvironmentName);
    Log.Information("===========================================");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação terminou inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}