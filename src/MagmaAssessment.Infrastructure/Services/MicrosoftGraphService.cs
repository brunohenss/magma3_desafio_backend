using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;
using MagmaAssessment.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MagmaAssessment.Infrastructure.Services
{
    public class MicrosoftGraphService : IMicrosoftGraphService
    {
        private readonly GraphServiceClient _graphClient;

        public MicrosoftGraphService(IConfiguration configuration)
        {
            var tenantId = configuration["MicrosoftGraph:TenantId"]!;
            var clientId = configuration["MicrosoftGraph:ClientId"]!;
            var clientSecret = configuration["MicrosoftGraph:ClientSecret"]!;

            _graphClient = new GraphServiceClient(
                new ClientSecretCredential(tenantId, clientId, clientSecret),
                new[] { "https://graph.microsoft.com/.default" }
            );
        }

        public async Task<List<User>> ObterUsuariosAsync()
        {
            var result = await _graphClient.Users.GetAsync();
            return result?.Value ?? new List<User>();
        }

        public async Task EnviarEmailAsync(string destinatario, string assunto, string corpo)
        {
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
        }

        public async Task<List<Group>> ObterGruposAsync()
        {
            var result = await _graphClient.Groups.GetAsync();
            return result?.Value ?? new List<Group>();
        }
    }
}
