using System.Threading.Channels;
using AmbientWeatherToDatadog.AmbientWeather.Realtime;
using AmbientWeatherToDatadog.Datadog;
using Serilog;
using Serilog.Events;

namespace AmbientWeatherToDatadog;

public static class Program
{
    public static async Task Main()
    {
        bool debug = Environment.GetEnvironmentVariable("AW_DEBUG") == "1";
        LogEventLevel logEventLevel = debug ? LogEventLevel.Debug : LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Is(logEventLevel)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .CreateLogger();

        Log.Information("Starting... AW_DEBUG={Debug}", debug);

        var channel = Channel.CreateBounded<DeviceMetrics>(
            new BoundedChannelOptions(capacity: 5)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true,
            });

        var ambientWeatherApplicationKey = Environment.GetEnvironmentVariable("AW_APP_KEY") ?? "";
        var ambientWeatherApiKey = Environment.GetEnvironmentVariable("AW_API_KEY") ?? "";
        var datadogApiKey = Environment.GetEnvironmentVariable("DD_API_KEY") ?? "";

        if (string.IsNullOrEmpty(ambientWeatherApplicationKey) ||
            string.IsNullOrEmpty(ambientWeatherApiKey) ||
            string.IsNullOrEmpty(datadogApiKey))
        {
            Log.Warning("Missing one of the following required env vars: AW_APP_KEY, AW_API_KEY, DD_API_KEY");
            return;
        }

        using var client = new AmbientWeatherRealtimeClient(channel.Writer, Log.Logger, ambientWeatherApplicationKey, ambientWeatherApiKey);
        await client.ConnectAsync();

        using var datadogClient = new DatadogMetricsClient(channel.Reader, Log.Logger, datadogApiKey);
        var cancellationTokenSource = new CancellationTokenSource();
        Task datadogTask = datadogClient.Start(cancellationTokenSource.Token);

        await Task.Delay(-1);

        cancellationTokenSource.Cancel();

        await datadogTask;
        await client.DisconnectAsync(CancellationToken.None);

        Log.Information("Exiting application...");
        await Log.CloseAndFlushAsync();
    }
}
