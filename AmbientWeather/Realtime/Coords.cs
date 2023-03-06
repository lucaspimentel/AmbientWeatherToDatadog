using System.Text.Json.Serialization;

namespace AmbientWeatherToDatadog.AmbientWeather.Realtime;

public sealed record Coords
{
    /// <summary>
    /// Geographic coordinates of the weather station
    /// </summary>
    [JsonPropertyName("coords")]
    public Coords2? Coord2 { get; init; }
}