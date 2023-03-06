using System.Text.Json.Serialization;

namespace AmbientWeatherToDatadog.Datadog;

#nullable disable

public class MetricsPayload
{
    [JsonPropertyName("series")]
    public List<Series> Series { get; set; }
}

public class Series
{
    [JsonPropertyName("metric")]
    public string MetricName { get; set; }

    // The available types are 0 (unspecified), 1 (count), 2 (rate), and 3 (gauge).
    [JsonPropertyName("type")]
    public SeriesType Type { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    // [JsonPropertyName("interval")]
    // public long? Interval { get; set; }

    [JsonPropertyName("points")]
    public List<Point> Points { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }
}

public class Point
{
    // Timestamps should be in POSIX time in seconds,
    // and cannot be more than ten minutes in the future or more than one hour in the past.
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

public enum SeriesType
{
    Unspecified = 0,
    Count = 1,
    Rate = 2,
    Gauge = 3,
}
