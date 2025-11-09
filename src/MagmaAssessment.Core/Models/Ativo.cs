using System.Text.Json.Serialization;

namespace MagmaAssessment.Core.Models;

public class Ativo
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string AssetType { get; set; } = string.Empty;

    [JsonPropertyName("SerialNumber")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("Agent.DataLastCommunication")]
    public DateTime? LastCommunicationAt { get; set; }

    [JsonPropertyName("Network.PublicIp")]
    public string? PublicIp { get; set; }

    // questao 1 - calcula dias sem comunicacao
    public int DaysSinceLastCommunication =>
        LastCommunicationAt.HasValue
        ? (int)(DateTime.UtcNow - LastCommunicationAt.Value).TotalDays
        : int.MaxValue;

    public bool IsInactive => DaysSinceLastCommunication > 60;
}