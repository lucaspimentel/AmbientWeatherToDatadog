using System.Text.Json.Serialization;

namespace AmbientWeatherToDatadog.AmbientWeather.Realtime;

public sealed record Info
{
    /// <summary>
    /// The name of the weather station configured in the AmbientWeather dashboard
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("elevation")]
    public double Elevation { get; init; }

    /// <summary>
    /// City Location
    /// </summary>
    [JsonPropertyName("coords")]
    public Coords? Coords { get; init; }

    [JsonPropertyName("geo")]
    public Geo? Geo { get; init; }
}