using MagmaAssessment.Core.Interfaces;
using MagmaAssessment.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IForce1Service, Force1Service>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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


app.Run();
