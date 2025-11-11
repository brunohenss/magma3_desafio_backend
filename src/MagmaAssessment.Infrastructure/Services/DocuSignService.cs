using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using MagmaAssessment.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MagmaAssessment.Infrastructure.Services;

// serviço de integração com DocuSign para assinatura eletronica de documentos
// implementa 3 metodos principais: enviar, obter e listar envelopes
public class DocuSignService : IDocuSignService
{
    private readonly ApiClient _apiClient;
    private readonly string _accountId;
    private readonly ILogger<DocuSignService> _logger;

    public DocuSignService(IConfiguration configuration, ILogger<DocuSignService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            var basePath = configuration["DocuSign:BasePath"]
                ?? throw new ArgumentNullException("DocuSign:BasePath não configurado");

            var accessToken = configuration["DocuSign:AccessToken"]
                ?? throw new ArgumentNullException("DocuSign:AccessToken não configurado");

            _accountId = configuration["DocuSign:AccountId"]
                ?? throw new ArgumentNullException("DocuSign:AccountId não configurado");

            _logger.LogInformation(
                "Inicializando DocuSign Service - BasePath: {BasePath}, AccountId: {AccountId}",
                basePath, _accountId);

            _apiClient = new ApiClient(basePath);

            _apiClient.Configuration.DefaultHeader.Add("Authorization", $"Bearer {accessToken}");

            _logger.LogInformation("DocuSign Service inicializado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar DocuSign Service");
            throw;
        }
    }


    // envia um documento para assinatura eletronica
    public async Task<EnvelopeSummary> EnviarDocumentoAssinaturaAsync(
        string emailDestinatario,
        string nomeDestinatario,
        byte[] documentoBytes,
        string nomeDocumento)
    {
        try
        {
            _logger.LogInformation(
                "Enviando documento para assinatura - Destinatário: {Email}, Documento: {Nome}",
                emailDestinatario, nomeDocumento);

            var envelopeApi = new EnvelopesApi(_apiClient.Configuration);

            var envDef = new EnvelopeDefinition
            {
                EmailSubject = $"Assine o documento: {nomeDocumento}",
                Documents = new List<Document>
                {
                    new Document
                    {
                        DocumentBase64 = Convert.ToBase64String(documentoBytes),
                        Name = nomeDocumento,
                        FileExtension = "pdf",
                        DocumentId = "1"
                    }
                },
                Recipients = new Recipients
                {
                    Signers = new List<Signer>
                    {
                        new Signer
                        {
                            Email = emailDestinatario,
                            Name = nomeDestinatario,
                            RecipientId = "1",
                            RoutingOrder = "1",
                            Tabs = new Tabs
                            {
                                SignHereTabs = new List<SignHere>
                                {
                                    new SignHere
                                    {
                                        DocumentId = "1",
                                        PageNumber = "1",
                                        XPosition = "100",
                                        YPosition = "100"
                                    }
                                }
                            }
                        }
                    }
                },
                Status = "sent"
            };

            var result = await envelopeApi.CreateEnvelopeAsync(_accountId, envDef);

            _logger.LogInformation(
                "Documento enviado com sucesso - EnvelopeId: {EnvelopeId}, Status: {Status}",
                result.EnvelopeId, result.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao enviar documento para assinatura - Destinatário: {Email}",
                emailDestinatario);
            throw;
        }
    }

    /// obtem detalhes de um envelope especifico
    public async Task<Envelope> ObterEnvelopeAsync(string envelopeId)
    {
        try
        {
            _logger.LogInformation("Obtendo detalhes do envelope: {EnvelopeId}", envelopeId);

            var envelopesApi = new EnvelopesApi(_apiClient.Configuration);
            var envelope = await envelopesApi.GetEnvelopeAsync(_accountId, envelopeId);

            _logger.LogInformation(
                "Envelope obtido - Status: {Status}, EmailSubject: {Subject}",
                envelope.Status, envelope.EmailSubject);

            return envelope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter envelope: {EnvelopeId}", envelopeId);
            throw;
        }
    }

    /// lista todos os envelopes da conta (ultimos 30 dias)
    public async Task<List<Envelope>> ListarEnvelopesAsync()
    {
        try
        {
            _logger.LogInformation("Listando todos os envelopes");

            var envelopesApi = new EnvelopesApi(_apiClient.Configuration);
            
            var options = new EnvelopesApi.ListStatusChangesOptions
            {
                fromDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd")
            };

            var results = await envelopesApi.ListStatusChangesAsync(_accountId, options);

            var envelopes = results.Envelopes?.ToList() ?? new List<Envelope>();

            _logger.LogInformation("Total de envelopes encontrados: {Count}", envelopes.Count);

            return envelopes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar envelopes");
            throw;
        }
    }
}