using DocuSign.eSign.Model;

namespace MagmaAssessment.Core.Interfaces
{
    public interface IDocuSignService
    {
        Task<List<Envelope>> ListarEnvelopesAsync(string accountId);
        Task<EnvelopeSummary> EnviarDocumentoAssinaturaAsync(string accountId, string emailDestinatario, string nomeDestinatario, string filePath);
        Task<Envelope> ObterEnvelopeAsync(string accountId, string envelopeId);
    }
}
