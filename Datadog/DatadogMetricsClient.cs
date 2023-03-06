using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AmbientWeatherToDatadog.AmbientWeather.Realtime;
using Serilog;

namespace AmbientWeatherToDatadog.Datadog;

public sealed class DatadogMetricsClient : IDisposable
{
    private readonly ChannelReader<DeviceMetrics> _channelReader;
    private readonly ILogger _logger;
    private readonly HttpClient _client = new();

    public DatadogMetricsClient(ChannelReader<DeviceMetrics> channelReader, ILogger logger)
    {
        _channelReader = channelReader;
        _logger = logger;

        _client.DefaultRequestHeaders.Add("DD-API-KEY", Constants.DatadogApiKey);
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        await foreach (var deviceMetrics in _channelReader.ReadAllAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var data = deviceMetrics.Data;

            if (data.EpochMilliseconds is not { } ms)
            {
                continue;
            }

            // POSIX milliseconds to seconds
            var timestampSeconds = ms / 1_000;

            var tags = new List<string>
                       {
                           "env:lucas.pimentel",
                           $"location.name:{deviceMetrics.DeviceName}",
                           $"location.mac:{deviceMetrics.DeviceMac}"
                       };

            var seriesList = new List<Series>(1);

            AddSeries(seriesList, timestampSeconds, "pws.tempf", data.OutdoorTemperatureFahrenheit, SeriesType.Gauge, "F", tags);
            AddSeries(seriesList, timestampSeconds, "pws.feelsLike", data.OutdoorFeelsLikeTemperatureFahrenheit, SeriesType.Gauge, "F", tags);
            AddSeries(seriesList, timestampSeconds, "pws.dewPoint", data.DewPointFahrenheit, SeriesType.Gauge, "F", tags);
            AddSeries(seriesList, timestampSeconds, "pws.windspeedmph", data.WindSpeedMph, SeriesType.Gauge, "mph", tags);
            AddSeries(seriesList, timestampSeconds, "pws.winddir", data.WindDirection, SeriesType.Gauge, null, tags);
            AddSeries(seriesList, timestampSeconds, "pws.windgustmph", data.WindGustMph, SeriesType.Gauge, "mph", tags);
            AddSeries(seriesList, timestampSeconds, "pws.maxdailygust", data.MaxDailyGust, SeriesType.Gauge, "mph", tags);
            AddSeries(seriesList, timestampSeconds, "pws.baromabsin", data.AbsoluteBarometricPressure, SeriesType.Gauge, "inHG", tags);
            AddSeries(seriesList, timestampSeconds, "pws.baromrelin", data.RelativeBarometricPressure, SeriesType.Gauge, "inHG", tags);
            AddSeries(seriesList, timestampSeconds, "pws.humidity", data.OutdoorHumidity, SeriesType.Gauge, null, tags);
            AddSeries(seriesList, timestampSeconds, "pws.solarradiation", data.SolarRadiation, SeriesType.Gauge, "W/m^2", tags);

            var metrics = new MetricsPayload { Series = seriesList };
            await Post(metrics, cancellationToken);
        }
    }

    private static void AddSeries(List<Series> seriesList, long timestamp, string name, double? value, SeriesType type, string? unit, List<string> tags)
    {
        if (value == null)
        {
            return;
        }

        var series = new Series
                     {
                         MetricName = name,
                         Type = type,
                         Unit = unit,
                         Points = new List<Point>
                                  {
                                      new()
                                      {
                                          Timestamp = timestamp,
                                          Value = (double)value
                                      }
                                  },
                         Tags = tags
                     };

        seriesList.Add(series);
    }

    public async Task Post(MetricsPayload payload, CancellationToken cancellationToken = default)
    {
        _logger.Information("Sending metrics to Datadog");

        string metricsJson = JsonSerializer.Serialize(payload);
        var content = new StringContent(metricsJson, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PostAsync("https://api.datadoghq.com/api/v2/series", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.Information("Response status code from Datadog: {StatusCode}", response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
