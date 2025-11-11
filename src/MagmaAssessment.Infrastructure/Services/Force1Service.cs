using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
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
    private readonly string _username;
    private readonly string _password;
    private readonly string _enterprise;

    public Force1Service(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<Force1Service> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _baseUrl = configuration["Force1:BaseUrl"] ?? "https://api.magma-3.com/v2/Force1";
        _username = configuration["Force1:Username"] ?? string.Empty;
        _password = configuration["Force1:Password"] ?? string.Empty;
        _enterprise = configuration["Force1:Enterprise"] ?? string.Empty;
        
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
        {
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_username}:{_password}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            _logger.LogInformation(
                "Autenticação configurada para usuário: {Username}, Enterprise: {Enterprise}",
                _username, _enterprise);
        }
        else
        {
            _logger.LogWarning("Credenciais Force1 não configuradas. Usando mocks");
        }
        
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => 
                r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message;
                    _logger.LogWarning(
                        "Retry {RetryCount} após {TimeSpan}s devido a: {Reason}", 
                        retryCount, timespan.TotalSeconds, reason);
                })
            .WrapAsync(
                Policy.HandleResult<HttpResponseMessage>(r => 
                    r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                    r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                    r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: 3,
                        durationOfBreak: TimeSpan.FromMinutes(1),
                        onBreak: (result, timespan) =>
                        {
                            _logger.LogWarning(
                                "Circuit breaker ativado por {Duration}s. API Force1 temporariamente indisponível.",
                                timespan.TotalSeconds);
                        },
                        onReset: () =>
                        {
                            _logger.LogInformation("Circuit breaker resetado. API Force1 disponível novamente.");
                        }));
    }

    public async Task<List<Ativo>> ObterTodosAtivos()
    {
        try
        {
            _logger.LogInformation("Iniciando busca de todos os ativos");

            HttpResponseMessage response;
            
            try
            {
                response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.GetAsync("GetAssets?pagination=0"));
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning(ex, "Circuit breaker aberto. Usando dados mock.");
                return ObterAtivosMock();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de conexão com API Force1. Usando dados mock.");
                return ObterAtivosMock();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Endpoint não encontrado (404). Verifique a URL. Usando mock.");
                return ObterAtivosMock();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Acesso negado (403). Verifique as credenciais. Usando mock.");
                return ObterAtivosMock();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Não autorizado (401). Credenciais inválidas. Usando mock.");
                return ObterAtivosMock();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API retornou status {StatusCode}. Usando mock.", response.StatusCode);
                return ObterAtivosMock();
            }

            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("API retornou resposta vazia. Usando mock.");
                return ObterAtivosMock();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var ativos = JsonSerializer.Deserialize<List<Ativo>>(content, options);

            if (ativos != null && ativos.Count > 0)
            {
                _logger.LogInformation("Sucesso! {Count} ativos obtidos da API REAL Force1", ativos.Count);
                return ativos;
            }

            _logger.LogWarning("API retornou array vazio. Usando mock.");
            return ObterAtivosMock();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao desserializar resposta. Usando mock.");
            return ObterAtivosMock();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar ativos. Usando mock.");
            return ObterAtivosMock();
        }
    }

    private List<Ativo> ObterAtivosMock()
    {
        _logger.LogInformation("Retornando dados MOCK para demonstração");
        
        return new List<Ativo>
        {
            new Ativo
            {
                Id = "758308234698",
                Name = "PC-Financeiro-Mock",
                AssetType = "Computer",
                SerialNumber = "SN-001",
                LastCommunicationAt = DateTime.UtcNow.AddDays(-90),
                PublicIp = "177.12.34.56"
            },
            new Ativo
            {
                Id = "2341108230901",
                Name = "Notebook-Suporte-Mock",
                AssetType = "Laptop",
                SerialNumber = "SN-002",
                LastCommunicationAt = DateTime.UtcNow.AddDays(-15),
                PublicIp = "187.54.23.11"
            },
            new Ativo
            {
                Id = "1231108230999",
                Name = "Desktop-RH-Mock",
                AssetType = "Desktop",
                SerialNumber = "SN-003",
                LastCommunicationAt = DateTime.UtcNow.AddDays(-75),
                PublicIp = "200.10.20.30"
            },
            new Ativo
            {
                Id = "8811108230321",
                Name = "Notebook-Dev-Mock",
                AssetType = "Laptop",
                SerialNumber = "SN-004",
                LastCommunicationAt = DateTime.UtcNow.AddDays(-5),
                PublicIp = "190.50.60.70"
            },
            new Ativo
            {
                Id = "7641108230001",
                Name = "Workstation-Design-Mock",
                AssetType = "Workstation",
                SerialNumber = "SN-005",
                LastCommunicationAt = DateTime.UtcNow.AddDays(-120),
                PublicIp = "177.80.90.100"
            }
        };
    }

    public async Task<List<Ativo>> ObterComputadoresInativos()
    {
        try
        {
            _logger.LogInformation("Buscando computadores inativos (>60 dias sem comunicação)");
            
            var todosAtivos = await ObterTodosAtivos();
            
            var computadoresInativos = todosAtivos
                .Where(a => 
                    !string.IsNullOrEmpty(a.AssetType) &&
                    (a.AssetType.Equals("Computer", StringComparison.OrdinalIgnoreCase) || 
                     a.AssetType.Equals("Desktop", StringComparison.OrdinalIgnoreCase) ||
                     a.AssetType.Equals("Laptop", StringComparison.OrdinalIgnoreCase) ||
                     a.AssetType.Equals("Workstation", StringComparison.OrdinalIgnoreCase)))
                .Where(a => a.IsInactive)
                .OrderByDescending(a => a.DaysSinceLastCommunication)
                .ToList();

            _logger.LogInformation(
                "Encontrados {Count} computadores inativos de {Total} ativos totais",
                computadoresInativos.Count, todosAtivos.Count);

            if (computadoresInativos.Any())
            {
                foreach (var computador in computadoresInativos)
                {
                    _logger.LogWarning(
                        "{Name} ({Type}) - {Days} dias sem comunicação - Última: {LastComm}",
                        computador.Name,
                        computador.AssetType,
                        computador.DaysSinceLastCommunication,
                        computador.LastCommunicationAt?.ToString("dd/MM/yyyy") ?? "Nunca");
                }
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

            HttpResponseMessage response;
            
            try
            {
                response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.GetAsync($"GetAsset/{id}"));
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit breaker aberto. Buscando no mock.");
                return ObterAtivosMock().FirstOrDefault(a => a.Id == id);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de conexão. Buscando no mock.");
                return ObterAtivosMock().FirstOrDefault(a => a.Id == id);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Ativo com ID {Id} não encontrado", id);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Erro ao buscar ativo. Status: {Status}. Buscando no mock.", 
                    response.StatusCode);
                return ObterAtivosMock().FirstOrDefault(a => a.Id == id);
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var ativo = JsonSerializer.Deserialize<Ativo>(content, options);

            _logger.LogInformation("Ativo encontrado: {Name}", ativo?.Name);
            return ativo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ativo por ID: {Id}. Buscando no mock.", id);
            return ObterAtivosMock().FirstOrDefault(a => a.Id == id);
        }
    }
}