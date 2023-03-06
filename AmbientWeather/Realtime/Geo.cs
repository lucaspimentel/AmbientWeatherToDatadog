using System.Text.Json.Serialization;

namespace AmbientWeatherToDatadog.AmbientWeather.Realtime;

public sealed record Geo
{
    /// <summary>
    /// The Type of Geo Coordinates. i.e. "Point"
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// A list of doubles containing the lat/lon coordinates
    /// coordinates[0] is longitude
    /// coordinates[1] is latitude
    /// </summary>
    [JsonPropertyName("coordinates")]
    public IReadOnlyList<double>? Coordinates { get; init; }
}
