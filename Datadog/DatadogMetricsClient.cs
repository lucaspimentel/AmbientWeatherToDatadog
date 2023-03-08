using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Serilog;

namespace AmbientWeatherToDatadog.Datadog;

public sealed class DatadogMetricsClient : IDisposable
{
    private readonly ChannelReader<DeviceMetrics> _channelReader;
    private readonly ILogger _logger;
    private readonly HttpClient _client = new();

    public DatadogMetricsClient(ChannelReader<DeviceMetrics> channelReader, ILogger logger, string apiKey)
    {
        logger.Debug("Initializing {Type}", typeof(DatadogMetricsClient).FullName);

        _channelReader = channelReader;
        _logger = logger;

        _client.DefaultRequestHeaders.Add("DD-API-KEY", apiKey);
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var deviceMetrics in _channelReader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Information("Datadog client: cancellation requested");
                    return;
                }

                _logger.Information("Datadog client: data received");

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

                var seriesList = new[]
                                 {
                                     // temp
                                     CreateSeries(timestampSeconds, "pws.tempf", data.OutdoorTemperatureFahrenheit, SeriesType.Gauge, "℉", tags),
                                     CreateSeries(timestampSeconds, "pws.feelsLike", data.OutdoorFeelsLikeTemperatureFahrenheit, SeriesType.Gauge, "℉", tags),

                                     // wind
                                     CreateSeries(timestampSeconds, "pws.windspeedmph", data.WindSpeedMph, SeriesType.Gauge, "mph", tags),
                                     CreateSeries(timestampSeconds, "pws.winddir", data.WindDirection, SeriesType.Gauge, null, tags),
                                     CreateSeries(timestampSeconds, "pws.windgustmph", data.WindGustMph, SeriesType.Gauge, "mph", tags),
                                     CreateSeries(timestampSeconds, "pws.maxdailygust", data.MaxDailyGust, SeriesType.Gauge, "mph", tags),

                                     // pressure
                                     CreateSeries(timestampSeconds, "pws.baromabsin", data.AbsoluteBarometricPressure, SeriesType.Gauge, "hg", tags),
                                     CreateSeries(timestampSeconds, "pws.baromrelin", data.RelativeBarometricPressure, SeriesType.Gauge, "hg", tags),

                                     // sun
                                     CreateSeries(timestampSeconds, "pws.solarradiation", data.SolarRadiation, SeriesType.Gauge, "W/m²", tags),
                                     CreateSeries(timestampSeconds, "pws.uv", data.UltravioletRadiationIndex, SeriesType.Gauge, "UVI", tags),

                                     // rain
                                     CreateSeries(timestampSeconds, "pws.hourlyrainin", data.HourlyRainfall, SeriesType.Gauge, unit: "in", tags),
                                     CreateSeries(timestampSeconds, "pws.dailyrainin", data.DailyRainfall, SeriesType.Gauge, unit: "in", tags),
                                     CreateSeries(timestampSeconds, "pws.weeklyrainin", data.WeeklyRainfall, SeriesType.Gauge, unit: "in", tags),
                                     CreateSeries(timestampSeconds, "pws.monthlyrainin", data.MonthlyRainfall, SeriesType.Gauge, unit: "in", tags),
                                     CreateSeries(timestampSeconds, "pws.yearlyrainin", data.YearlyRainfall, SeriesType.Gauge, unit: "in", tags),
                                     CreateSeries(timestampSeconds, "pws.eventrainin", data.EventRainfall, SeriesType.Gauge, unit: "in", tags),
                                     CreateSeries(timestampSeconds, "pws.totalrainin", data.TotalRainfall, SeriesType.Gauge, unit: "in", tags),

                                     // misc
                                     CreateSeries(timestampSeconds, "pws.humidity", data.OutdoorHumidity, SeriesType.Gauge, null, tags),
                                     CreateSeries(timestampSeconds, "pws.dewPoint", data.DewPointFahrenheit, SeriesType.Gauge, "℉", tags),
                                     CreateSeries(timestampSeconds, "pws.battout", data.BatteryLowIndicator, SeriesType.Gauge, unit: null, tags), // 1 = good, 0 = bad
                                 };

                var metrics = new MetricsPayload { Series = seriesList.Where(s => s != null) };
                await Post(metrics, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception e)
        {
            _logger.Error(e, "Datadog client: error reading from metrics channel");
        }
    }

    private static Series? CreateSeries(long timestamp, string name, double? value, SeriesType type, string? unit, List<string> tags)
    {
        if (value == null)
        {
            return null;
        }

        return new Series
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
    }

    private async Task Post(MetricsPayload payload, CancellationToken cancellationToken = default)
    {
        _logger.Information("Datadog client: sending metrics");

        string metricsJson = JsonSerializer.Serialize(payload);
        _logger.Debug("Datadog client: request content is {RequestContent}", metricsJson);

        using var requestContent = new StringContent(metricsJson, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await _client.PostAsync("https://api.datadoghq.com/api/v2/series", requestContent, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.Debug(
                "Datadog client: response status code is {StatusName} ({StatusCode}), content is {ResponseContent}",
                response.StatusCode,
                (int)response.StatusCode,
                responseContent);

            response.EnsureSuccessStatusCode();
            _logger.Information("Datadog client: metrics sent successfully");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Datadog client: error sending metrics");
        }
    }

    public void Dispose()
    {
        if (_logger != null!)
        {
            _logger.Debug("Disposing {Type}", typeof(DatadogMetricsClient).FullName);
        }

        _client.Dispose();
    }
}
