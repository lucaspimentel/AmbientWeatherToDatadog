using System.Text.Json.Serialization;

namespace AmbientWeatherToDatadog.AmbientWeather.Realtime;

public sealed record Root
{
    /// <summary>
    /// List of devices belonging to the user
    /// </summary>
    [JsonPropertyName("devices")]
    public ICollection<UserDevice>? Devices { get; init; }

    /// <summary>
    /// List of invalid API keys
    /// After sending the 'unsubscribe' command, ambient weather returns a list of invalid API keys
    /// </summary>
    [JsonPropertyName("invalidApiKeys")]
    public ICollection<string?>? InvalidAPIKeys { get; init; }

    /// <summary>
    /// The returned event type
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; init; }

    // TODO: Need to add an "error" field to store the error message that the Realtime API returns if API or Application keys are invalid, null, or in the incorrect serialized format
}