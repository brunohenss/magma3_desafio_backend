using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using MagmaAssessment.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MagmaAssessment.Infrastructure.Services;


// serviço de integração com microsoft graph api
// implementa 3 metodos: ObterUsuarios, EnviarEmail, ObterGrupos
public class MicrosoftGraphService : IMicrosoftGraphService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<MicrosoftGraphService> _logger;

    public MicrosoftGraphService(
        IConfiguration configuration,
        ILogger<MicrosoftGraphService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            var tenantId = configuration["MicrosoftGraph:TenantId"]
                ?? throw new ArgumentNullException("MicrosoftGraph:TenantId não configurado");

            var clientId = configuration["MicrosoftGraph:ClientId"]
                ?? throw new ArgumentNullException("MicrosoftGraph:ClientId não configurado");

            var clientSecret = configuration["MicrosoftGraph:ClientSecret"]
                ?? throw new ArgumentNullException("MicrosoftGraph:ClientSecret não configurado");

            _logger.LogInformation(
                "Inicializando Microsoft Graph Service - TenantId: {TenantId}, ClientId: {ClientId}",
                tenantId, clientId);

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            _graphClient = new GraphServiceClient(
                credential,
                new[] { "https://graph.microsoft.com/.default" }
            );

            _logger.LogInformation("Microsoft Graph Service inicializado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar Microsoft Graph Service");
            throw;
        }
    }

    public async Task<List<User>> ObterUsuariosAsync()
    {
        try
        {
            _logger.LogInformation("Obtendo lista de usuários do Microsoft Graph");

            var result = await _graphClient.Users.GetAsync();
            var users = result?.Value ?? new List<User>();

            _logger.LogInformation("Total de usuários obtidos: {Count}", users.Count);

            if (users.Any())
            {
                _logger.LogDebug(
                    "Primeiros 3 usuários: {Users}",
                    string.Join(", ", users.Take(3).Select(u => u.DisplayName)));
            }

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter usuários do Microsoft Graph");
            throw;
        }
    }

    public async Task EnviarEmailAsync(string destinatario, string assunto, string corpo)
    {
        try
        {
            _logger.LogInformation(
                "Enviando email via Microsoft Graph - Destinatário: {Destinatario}, Assunto: {Assunto}",
                destinatario, assunto);

            var message = new Message
            {
                Subject = assunto,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = corpo
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = destinatario
                        }
                    }
                }
            };

            var requestBody = new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true
            };

            await _graphClient.Me.SendMail.PostAsync(requestBody);

            _logger.LogInformation(
                "Email enviado com sucesso para: {Destinatario}",
                destinatario);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao enviar email via Microsoft Graph para: {Destinatario}",
                destinatario);
            throw;
        }
    }

    public async Task<List<Group>> ObterGruposAsync()
    {
        try
        {
            _logger.LogInformation("Obtendo lista de grupos do Microsoft Graph");

            var result = await _graphClient.Groups.GetAsync();
            var groups = result?.Value ?? new List<Group>();

            _logger.LogInformation("Total de grupos obtidos: {Count}", groups.Count);

            if (groups.Any())
            {
                _logger.LogDebug(
                    "Primeiros 3 grupos: {Groups}",
                    string.Join(", ", groups.Take(3).Select(g => g.DisplayName)));
            }

            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter grupos do Microsoft Graph");
            throw;
        }
    }
}