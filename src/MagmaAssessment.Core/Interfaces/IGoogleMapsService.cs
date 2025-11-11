namespace MagmaAssessment.Core.Interfaces;

public interface IGoogleMapsService
{
    Task<string> ObterCoordenadasAsync(string endereco);
}
