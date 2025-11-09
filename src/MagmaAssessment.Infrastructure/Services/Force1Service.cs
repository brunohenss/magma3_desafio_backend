using System.Net.Http.Headers;
using System.Text.Json;
using MagmaAssessment.Core.Interfaces;
using MagmaAssessment.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace MagmaAssessment.Infrastructure.Services;

public class Force1Service : IForce1Service
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Force1Service> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly string _baseUrl;

    public Force1Service(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<Force1Service> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _baseUrl = configuration["Force1:BaseUrl"] ?? "https://api.magma-3.com/v2/Force1/";
        
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        
        var apiKey = configuration["Force1:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message;
                    _logger.LogWarning(
                        "Retry {RetryCount} após {TimeSpan}s. Motivo: {Reason}", 
                        retryCount, timespan.TotalSeconds, reason);
                })
            .WrapAsync(
                Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .CircuitBreakerAsync(
                        3,
                        TimeSpan.FromMinutes(1),
                        onBreak: (result, timespan) =>
                        {
                            _logger.LogError(
                                "circuit breaker ativado por {TimeSpan}s", 
                                timespan.TotalSeconds);
                        },
                        onReset: () =>
                        {
                            _logger.LogInformation("circuit breaker resetado");
                        }));
    }

    public async Task<List<Ativo>> ObterTodosAtivos()
    {
      try
      {
        _logger.LogInformation("Iniciando busca de todos os ativos");

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync("GetAssets?pagination=0"));

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("Acesso negado à API Force1. Usando dados simulados (mock).");

            // mock de ativos caso nao autentique na api (fim didatico)
            return new List<Ativo>
            {
                new Ativo
                {
                    Id = "1",
                    Name = "PC-Financeiro",
                    AssetType = "Computer",
                    LastCommunicationAt = DateTime.UtcNow.AddDays(-90), // inativo
                    PublicIp = "177.12.34.56"
                },
                new Ativo
                {
                    Id = "2",
                    Name = "Notebook-Suporte",
                    AssetType = "Laptop",
                    LastCommunicationAt = DateTime.UtcNow.AddDays(-15), // ativo
                    PublicIp = "187.54.23.11"
                }
            };
        }

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var ativos = JsonSerializer.Deserialize<List<Ativo>>(content);

            if (ativos != null && ativos.Count > 0)
            {
                _logger.LogInformation("Busca concluída. {Count} ativos encontrados", ativos.Count);
                return ativos;
            }

            _logger.LogWarning("Nenhum ativo encontrado na resposta da API");
            return new List<Ativo>();
        }

        _logger.LogWarning("Resposta inválida da API. StatusCode: {StatusCode}", response.StatusCode);
        return new List<Ativo>();
    }
    catch (BrokenCircuitException ex)
    {
        _logger.LogError(ex, "Circuit breaker está aberto. Serviço temporariamente indisponível");
        throw new InvalidOperationException("Serviço Force1 temporariamente indisponível", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao buscar ativos da API Force1");
        throw;
    }
    }


    public async Task<List<Ativo>> ObterComputadoresInativos()
    {
        try
        {
            _logger.LogInformation(
                "Buscando computadores inativos (>60 dias sem comunicação)");
            
            var todosAtivos = await ObterTodosAtivos();
            
            var computadoresInativos = todosAtivos
                .Where(a => 
                    a.AssetType?.ToLower() == "computer" || 
                    a.AssetType?.ToLower() == "desktop" ||
                    a.AssetType?.ToLower() == "laptop" ||
                    a.AssetType?.ToLower() == "workstation")
                .Where(a => a.IsInactive)
                .OrderByDescending(a => a.DaysSinceLastCommunication)
                .ToList();

            _logger.LogInformation(
                "Encontrados {Count} computadores inativos de {Total} ativos totais",
                computadoresInativos.Count, todosAtivos.Count);

            foreach (var computador in computadoresInativos)
            {
                _logger.LogWarning(
                    "Computador inativo: {Name} - {Days} dias sem comunicação - " +
                    "Última comunicação: {LastComm}",
                    computador.Name,
                    computador.DaysSinceLastCommunication,
                    computador.LastCommunicationAt?.ToString("dd/MM/yyyy") ?? "Nunca");
            }

            return computadoresInativos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar computadores inativos");
            throw;
        }
    }

    public async Task<Ativo?> ObterAtivoPorId(string id)
    {
        try
        {
            _logger.LogInformation("Buscando ativo com ID: {Id}", id);

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _httpClient.GetAsync($"/GetAsset/{id}"));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var ativo = JsonSerializer.Deserialize<Ativo>(content);

                _logger.LogInformation("Ativo encontrado: {Name}", ativo?.Name);
                return ativo;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Ativo com ID {Id} não encontrado", id);
                return null;
            }

            throw new HttpRequestException(
                $"Erro ao buscar ativo. StatusCode: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ativo por ID: {Id}", id);
            throw;
        }
    }
}