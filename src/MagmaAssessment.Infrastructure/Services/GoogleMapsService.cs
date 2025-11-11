using GoogleMaps.LocationServices;
using Microsoft.Extensions.Configuration;
using MagmaAssessment.Core.Interfaces;
using System.Threading.Tasks;

namespace MagmaAssessment.Infrastructure.Services;

public class GoogleMapsService : IGoogleMapsService
{
    private readonly string _apiKey;

    public GoogleMapsService(IConfiguration configuration)
    {
        _apiKey = configuration["GoogleMaps:ApiKey"] 
                  ?? throw new ArgumentNullException("GoogleMaps:ApiKey não configurada");
    }

    // Implementa a interface
    public async Task<string> ObterCoordenadasAsync(string endereco)
    {
        var locationService = new GoogleLocationService(_apiKey);
        var point = locationService.GetLatLongFromAddress(endereco);
        if (point == null) return null;

        return $"{point.Latitude},{point.Longitude}";
    }

    // Métodos adicionais opcionais
    public (double Latitude, double Longitude)? ObterCoordenadas(string endereco)
    {
        var locationService = new GoogleLocationService(_apiKey);
        var point = locationService.GetLatLongFromAddress(endereco);
        return point != null ? (point.Latitude, point.Longitude) : null;
    }

    public string? ObterEnderecoPorCoordenadas(double latitude, double longitude)
    {
        var locationService = new GoogleLocationService(_apiKey);
        var address = locationService.GetAddressFromLatLang(latitude, longitude);
        return address?.Address;
    }

    public double CalcularDistanciaKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Raio da Terra em km
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) *
                Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
