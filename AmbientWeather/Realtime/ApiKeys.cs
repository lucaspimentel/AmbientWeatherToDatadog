using System.Text.Json.Serialization;

namespace AmbientWeatherToDatadog.AmbientWeather.Realtime;

public sealed class ApiKeys
{
    public ApiKeys(string value) : this(new[] { value })
    {
    }

    public ApiKeys(IEnumerable<string> value)
    {
        Value = value;
    }

    [JsonPropertyName("apiKeys")]
    public IEnumerable<string> Value { get; }
}
