namespace MagmaAssessment.Core.Interfaces;

public interface IGoogleMapsService
{
    Task<string?> ObterCoordenadasAsync(string endereco);

    Task<(double Latitude, double Longitude)?> ObterCoordenadasTuplaAsync(string endereco);

    Task<string?> ObterEnderecoPorCoordenadasAsync(double latitude, double longitude);

    double CalcularDistanciaKm(double lat1, double lon1, double lat2, double lon2);
}