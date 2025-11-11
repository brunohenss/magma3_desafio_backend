using DocuSign.eSign.Model;

namespace MagmaAssessment.Core.Interfaces;

public interface IDocuSignService
{
    Task<EnvelopeSummary> EnviarDocumentoAssinaturaAsync(
        string emailDestinatario,
        string nomeDestinatario,
        byte[] documentoBytes,
        string nomeDocumento);

    Task<Envelope> ObterEnvelopeAsync(string envelopeId);

    Task<List<Envelope>> ListarEnvelopesAsync();
}