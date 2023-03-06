using System.Text.Json.Serialization;

namespace AmbientWeatherToDatadog.AmbientWeather.Realtime;

public sealed record Coords2
{
    /// <summary>
    /// Latitude of the weather station
    /// </summary>
    [JsonPropertyName("lat")]
    public double Latitude { get; init; }

    /// <summary>
    /// Longitude of the weather station
    /// </summary>
    [JsonPropertyName("lon")]
    public double Longitude { get; init; }
}