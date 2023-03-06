using System.Text.Json.Serialization;

namespace AmbientWeatherToDatadog.AmbientWeather.Rest;

#nullable disable

public class Device
{
    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; }

    [JsonPropertyName("lastData")]
    public Data LastData { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class Info
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("coords")]
    public Location Coords { get; set; }
}

public class Location
{
    [JsonPropertyName("coords")]
    public Coordinates Coordinates { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("location")]
    public string LocationName { get; set; }

    [JsonPropertyName("elevation")]
    public double? Elevation { get; set; }

    [JsonPropertyName("geo")]
    public Geo Geo { get; set; }
}

public class Coordinates
{
    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lon")]
    public double? Lon { get; set; }
}

public class Geo
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("coordinates")]
    public List<double?> Coordinates { get; set; }
}

public class Data
{
    [JsonPropertyName("dateutc")]
    public long? Dateutc { get; set; }

    [JsonPropertyName("tempf")]
    public double? Tempf { get; set; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    [JsonPropertyName("windspeedmph")]
    public double? Windspeedmph { get; set; }

    [JsonPropertyName("windgustmph")]
    public double? Windgustmph { get; set; }

    [JsonPropertyName("maxdailygust")]
    public double? Maxdailygust { get; set; }

    [JsonPropertyName("winddir")]
    public double? Winddir { get; set; }

    [JsonPropertyName("uv")]
    public double? Uv { get; set; }

    [JsonPropertyName("solarradiation")]
    public double? Solarradiation { get; set; }

    [JsonPropertyName("hourlyrainin")]
    public double? Hourlyrainin { get; set; }

    [JsonPropertyName("eventrainin")]
    public double? Eventrainin { get; set; }

    [JsonPropertyName("dailyrainin")]
    public double? Dailyrainin { get; set; }

    [JsonPropertyName("weeklyrainin")]
    public double? Weeklyrainin { get; set; }

    [JsonPropertyName("monthlyrainin")]
    public double? Monthlyrainin { get; set; }

    [JsonPropertyName("totalrainin")]
    public double? Totalrainin { get; set; }

    [JsonPropertyName("battout")]
    public double? Battout { get; set; }

    [JsonPropertyName("tempinf")]
    public double? Tempinf { get; set; }

    [JsonPropertyName("humidityin")]
    public double? Humidityin { get; set; }

    [JsonPropertyName("baromrelin")]
    public double? Baromrelin { get; set; }

    [JsonPropertyName("baromabsin")]
    public double? Baromabsin { get; set; }

    [JsonPropertyName("feelsLike")]
    public double? FeelsLike { get; set; }

    [JsonPropertyName("dewPoint")]
    public double? DewPoint { get; set; }

    [JsonPropertyName("feelsLikein")]
    public double? FeelsLikein { get; set; }

    [JsonPropertyName("dewPointin")]
    public double? DewPointin { get; set; }

    [JsonPropertyName("lastRain")]
    public DateTime? LastRain { get; set; }

    [JsonPropertyName("tz")]
    public string Tz { get; set; }

    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }
}
