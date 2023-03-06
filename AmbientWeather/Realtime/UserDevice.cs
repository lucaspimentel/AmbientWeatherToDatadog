using System.Text.Json.Serialization;

namespace AmbientWeatherToDatadog.AmbientWeather.Realtime;

public sealed record UserDevice
{
    /// <summary>
    /// Weather Station Mac Address
    /// </summary>
    [JsonPropertyName("macAddress")]
    public string? MacAddress { get; init; }

    /// <summary>
    /// Instance of <see cref="Info"/> class
    /// </summary>
    [JsonPropertyName("info")]
    public Info? Info { get; init; }

    /// <summary>
    /// Instance of <see cref="Device"/> class
    /// </summary>
    [JsonPropertyName("lastData")]
    public Device? LastData { get; init; }

    /// <summary>
    /// The API Key used for the subscribe command
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; init; }
}