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

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Magma3 Assessment API - Desafio técnico para processo seletivo",
        Version = "v1",
        Description = "API desenvolvida para o desafio técnico Magma3 - Backend Developer",
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

// registra serviços e repositórios
builder.Services.AddHttpClient<IForce1Service, Force1Service>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Magma3 Assessment API v1");
        options.RoutePrefix = string.Empty; // swagger na raiz
    });
}

app.UseHttpsRedirection();

app.MapControllers();

// health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    service = "Magma3 Backend Assessment"
}))
.WithName("HealthCheck")
.WithOpenApi();

app.MapGet("/ativos", async (IForce1Service force1Service) =>
{
    var ativos = await force1Service.ObterTodosAtivos();
    return Results.Ok(ativos);
})
.WithName("GetAtivos")
.WithOpenApi();

app.MapGet("/computadores-inativos", async (IForce1Service force1Service) =>
{
    var computadoresInativos = await force1Service.ObterComputadoresInativos();
    return Results.Ok(computadoresInativos);
})
.WithName("GetComputadoresInativos")
.WithOpenApi();

app.MapGet("/ativo/{id}", async (string id, IForce1Service force1Service) =>
{
    var ativo = await force1Service.ObterAtivoPorId(id);
    return ativo is not null ? Results.Ok(ativo) : Results.NotFound();
})
.WithName("GetAtivoPorId")
.WithOpenApi();

try
{
    Log.Information("Iniciando aplicação Magma3 Backend Assessment");
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