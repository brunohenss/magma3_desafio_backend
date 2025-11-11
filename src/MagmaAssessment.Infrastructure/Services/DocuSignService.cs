using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using MagmaAssessment.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MagmaAssessment.Infrastructure.Services;

public class DocuSignService : IDocuSignService
{
    private readonly DocuSignClient _client;

    public DocuSignService(IConfiguration configuration)
    {
        var basePath = configuration["DocuSign:BasePath"];
        _client = new DocuSignClient(basePath);
    }

    public async Task<EnvelopeSummary> EnviarDocumentoAssinaturaAsync(string accountId, string emailDestinatario, string nomeDestinatario, string filePath)
    {
        var envelopeApi = new EnvelopesApi(_client);

        var envDef = new EnvelopeDefinition
        {
            EmailSubject = "Assine este documento, por favor",
            Documents = new List<Document>
            {
                new Document
                {
                    DocumentBase64 = Convert.ToBase64String(File.ReadAllBytes(filePath)),
                    Name = Path.GetFileName(filePath),
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
                                    AnchorString = "Assine aqui",
                                    AnchorUnits = "pixels",
                                    AnchorXOffset = "100",
                                    AnchorYOffset = "10"
                                }
                            }
                        }
                    }
                }
            },
            Status = "sent"
        };

        return await envelopeApi.CreateEnvelopeAsync(accountId, envDef);
    }

    public async Task<Envelope> ObterEnvelopeAsync(string accountId, string envelopeId)
    {
        var envelopesApi = new EnvelopesApi(_client);
        return await envelopesApi.GetEnvelopeAsync(accountId, envelopeId);
    }

    public async Task<List<Envelope>> ListarEnvelopesAsync(string accountId)
    {
        var envelopesApi = new EnvelopesApi(_client);
        var results = await envelopesApi.ListStatusChangesAsync(accountId);
        return results.Envelopes?.ToList() ?? new List<Envelope>();
    }
}
