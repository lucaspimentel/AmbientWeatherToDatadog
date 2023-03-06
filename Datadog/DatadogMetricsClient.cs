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
            var tempstampSeconds = ms / 1_000;

            var tags = new List<string>
                       {
                           "env:lucas.pimentel",
                           $"location.name:{deviceMetrics.DeviceName}",
                           $"location.mac:{deviceMetrics.DeviceMac}"
                       };

            var seriesList = new List<Series>(1);

            if (data.OutdoorTemperatureFahrenheit is { } temp)
            {
                var series = new Series
                             {
                                 MetricName = @"pws.tempf",
                                 Type = SeriesType.Gauge,
                                 Unit = "F",
                                 Points = new List<Point>
                                          {
                                              new()
                                              {
                                                  Timestamp = tempstampSeconds,
                                                  Value = temp
                                              }
                                          },
                                 Tags = tags
                             };

                seriesList.Add(series);
            }

            if (data.WindSpeedMph is { } windSpeed)
            {
                var series = new Series
                             {
                                 MetricName = @"pws.windspeedmph",
                                 Type = SeriesType.Gauge,
                                 Unit = "mph",
                                 Points = new List<Point>
                                          {
                                              new()
                                              {
                                                  Timestamp = tempstampSeconds,
                                                  Value = windSpeed
                                              }
                                          },
                                 Tags = tags
                             };

                seriesList.Add(series);
            }

            if (data.WindGustMph is { } windGust)
            {
                var series = new Series
                             {
                                 MetricName = @"pws.windgustmph",
                                 Type = SeriesType.Gauge,
                                 Unit = "mph",
                                 Points = new List<Point>
                                          {
                                              new()
                                              {
                                                  Timestamp = tempstampSeconds,
                                                  Value = windGust
                                              }
                                          },
                                 Tags = tags
                             };

                seriesList.Add(series);
            }

            var metrics = new MetricsPayload { Series = seriesList };
            await Post(metrics, cancellationToken);
        }
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
