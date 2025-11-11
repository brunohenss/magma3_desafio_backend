using GoogleMaps.LocationServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MagmaAssessment.Core.Interfaces;

namespace MagmaAssessment.Infrastructure.Services;

// serviço de integração com google maps api (geocoding)
// implementa 3 metodos: geocoding, reverse geocoding e calculo de distância
public class GoogleMapsService : IGoogleMapsService
{
    private readonly string _apiKey;
    private readonly ILogger<GoogleMapsService> _logger;

    public GoogleMapsService(
        IConfiguration configuration,
        ILogger<GoogleMapsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["GoogleMaps:ApiKey"]
                  ?? throw new ArgumentNullException("GoogleMaps:ApiKey não configurada");

        _logger.LogInformation("Google Maps Service inicializado com sucesso");
    }

    // Converte um endereço em coordenadas geograficas
    public async Task<string?> ObterCoordenadasAsync(string endereco)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(endereco))
            {
                _logger.LogWarning("Tentativa de geocoding com endereço vazio");
                return null;
            }

            _logger.LogInformation("Obtendo coordenadas para endereço: {Endereco}", endereco);

            var coordenadas = await Task.Run(() =>
            {
                var locationService = new GoogleLocationService(_apiKey);
                var point = locationService.GetLatLongFromAddress(endereco);

                if (point == null)
                {
                    _logger.LogWarning("Coordenadas não encontradas para: {Endereco}", endereco);
                    return null;
                }

                var resultado = $"{point.Latitude},{point.Longitude}";
                _logger.LogInformation(
                    "Coordenadas obtidas - Endereço: {Endereco}, Lat/Lng: {Coordenadas}",
                    endereco, resultado);

                return resultado;
            });

            return coordenadas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter coordenadas para endereço: {Endereco}", endereco);
            return null;
        }
    }

    public async Task<(double Latitude, double Longitude)?> ObterCoordenadasTuplaAsync(string endereco)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(endereco))
            {
                _logger.LogWarning("Tentativa de geocoding com endereço vazio");
                return null;
            }

            _logger.LogInformation("Obtendo coordenadas (tupla) para: {Endereco}", endereco);

            var resultado = await Task.Run(() =>
            {
                var locationService = new GoogleLocationService(_apiKey);
                var point = locationService.GetLatLongFromAddress(endereco);

                if (point == null)
                {
                    _logger.LogWarning("Coordenadas não encontradas para: {Endereco}", endereco);
                    return ((double, double)?)null;
                }

                _logger.LogInformation(
                    "Coordenadas obtidas - Lat: {Lat}, Lng: {Lng}",
                    point.Latitude, point.Longitude);

                return (point.Latitude, point.Longitude);
            });

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter coordenadas para: {Endereco}", endereco);
            return null;
        }
    }

    public async Task<string?> ObterEnderecoPorCoordenadasAsync(double latitude, double longitude)
    {
        try
        {
            _logger.LogInformation(
                "Obtendo endereço para coordenadas - Lat: {Lat}, Lng: {Lng}",
                latitude, longitude);

            var endereco = await Task.Run(() =>
            {
                var locationService = new GoogleLocationService(_apiKey);
                var address = locationService.GetAddressFromLatLang(latitude, longitude);

                if (address == null || string.IsNullOrWhiteSpace(address.Address))
                {
                    _logger.LogWarning(
                        "Endereço não encontrado para coordenadas - Lat: {Lat}, Lng: {Lng}",
                        latitude, longitude);
                    return null;
                }

                _logger.LogInformation(
                    "Endereço obtido - Coordenadas: ({Lat},{Lng}), Endereço: {Endereco}",
                    latitude, longitude, address.Address);

                return address.Address;
            });

            return endereco;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao obter endereço para coordenadas - Lat: {Lat}, Lng: {Lng}",
                latitude, longitude);
            return null;
        }
    }

    public double CalcularDistanciaKm(double lat1, double lon1, double lat2, double lon2)
    {
        try
        {
            _logger.LogInformation(
                "Calculando distância - P1: ({Lat1},{Lon1}) -> P2: ({Lat2},{Lon2})",
                lat1, lon1, lat2, lon2);

            const double R = 6371; // Raio da Terra em quilômetros

            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) *
                    Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distancia = R * c;

            _logger.LogInformation("Distância calculada: {Distancia:F2} km", distancia);

            return distancia;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular distância entre coordenadas");
            throw;
        }
    }
}