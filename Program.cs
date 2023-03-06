using System.Threading.Channels;
using AmbientWeatherToDatadog.AmbientWeather.Realtime;
using AmbientWeatherToDatadog.Datadog;
using Serilog;

namespace AmbientWeatherToDatadog;

public static class Program
{
    public static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .CreateLogger();

        var channel = Channel.CreateBounded<DeviceMetrics>(
            new BoundedChannelOptions(capacity: 5)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true,
            });

        using var client = new AmbientWeatherRealtimeClient(channel.Writer, Log.Logger, Constants.AmbientWeatherApplicationKey, Constants.AmbientWeatherApiKey);
        await client.ConnectAsync(CancellationToken.None);

        using var datadogClient = new DatadogMetricsClient(channel.Reader, Log.Logger);
        var cancellationTokenSource = new CancellationTokenSource();
        Task datadogTask = datadogClient.Start(cancellationTokenSource.Token);

        Console.WriteLine("Press [ENTER] to stop.");
        Console.ReadLine();

        cancellationTokenSource.Cancel();

        await datadogTask;
        await client.DisconnectAsync(CancellationToken.None);

        Log.Information("Exiting application...");
        await Log.CloseAndFlushAsync();
    }
}
