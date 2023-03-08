using System.Threading.Channels;
using Serilog;
using Serilog.Events;
using SocketIOClient;

namespace AmbientWeatherToDatadog.AmbientWeather.Realtime;

// https://rt2.ambientweather.net/?applicationKey=&apiKey=

public sealed class AmbientWeatherRealtimeClient : IDisposable
{
    private const string BaseUrl = "https://rt2.ambientweather.net";

    private readonly ChannelWriter<DeviceMetrics> _channelWriter;
    private readonly ILogger _logger;
    private readonly string _apiKey;
    private readonly SocketIO _client;
    private readonly Dictionary<string, string> _deviceNames = new(1);
    private readonly EventHandler _onConnectedHandler;
    private readonly EventHandler<string> _onDisconnectedHandler;

    public AmbientWeatherRealtimeClient(ChannelWriter<DeviceMetrics> channelWriter, ILogger logger, string applicationKey, string apiKey)
    {
        logger.Debug("Initializing {Type}", typeof(AmbientWeatherRealtimeClient).FullName);

        _channelWriter = channelWriter;
        _logger = logger;
        _apiKey = apiKey;

        var client = new SocketIO(
            BaseUrl,
            new SocketIOOptions
            {
                EIO = EngineIO.V4,
                Query = new Dictionary<string, string>
                        {
                            { "api", "1" },
                            { "applicationKey", applicationKey }
                        },
                Reconnection = true,
                ReconnectionDelay = 5000,
                ReconnectionDelayMax = 30000,
            });

        _onConnectedHandler = async (_, _) => await OnConnected();
        _onDisconnectedHandler = (_, message) => OnDisconnected(message);

        client.OnConnected += _onConnectedHandler;
        client.OnDisconnected += _onDisconnectedHandler;

        client.On("subscribed", OnSubscribed);
        client.On("data", OnData);

        _client = client;
    }

    public async Task ConnectAsync()
    {
        _logger.Information("Ambient Weather: connecting to {Url}", BaseUrl);
        await _client.ConnectAsync();
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        const string disconnectCommand = "disconnect";
        const string unsubscribeCommand = "unsubscribe";

        _logger.Information("Ambient Weather: unsubscribing");
        await _client.DisconnectAsync();

        _client.Off("subscribed");
        _client.Off("data");

        _logger.Debug("Ambient Weather: sending {Command} command", unsubscribeCommand);
        await _client.EmitAsync(unsubscribeCommand, cancellationToken, new ApiKeys(_apiKey));

        _logger.Debug("Ambient Weather: sending {Command} command", disconnectCommand);
        await _client.EmitAsync(disconnectCommand, cancellationToken);

        _logger.Information("Ambient Weather: disconnecting");
        await _client.DisconnectAsync();
    }

    private async Task OnConnected()
    {
        _logger.Information("Ambient Weather: connected, subcribing to data messages");

        const string connectCommand = "connect";
        const string subscribeCommand = "subscribe";

        _logger.Debug("Ambient Weather: sending {Command} command", connectCommand);
        await _client.EmitAsync(connectCommand);

        _logger.Debug("Ambient Weather: sending {Command} command", subscribeCommand);
        await _client.EmitAsync(subscribeCommand, new ApiKeys(_apiKey));
    }

    private void OnDisconnected(string message)
    {
        _logger.Information("Ambient Weather: disconnected, {Message}", message);
    }

    private void OnSubscribed(SocketIOResponse response)
    {
        var value = response.GetValue<Root>();
        var userDevice = value.Devices?.FirstOrDefault();

        if (_logger.IsEnabled(LogEventLevel.Debug))
        {
            _logger.Debug("Ambient Weather: subscription message received, {Data}", response.GetValue());
        }
        else if (_logger.IsEnabled(LogEventLevel.Information))
        {
            _logger.Information("Ambient Weather: subscription message received, {Method} {Name} ({Mac})", value.Method, userDevice?.Info?.Name, userDevice?.MacAddress);
        }

        if (userDevice is { MacAddress: { } mac, Info.Name: { } name, LastData: { } device })
        {
            _deviceNames[mac] = name;
            _channelWriter.TryWrite(new DeviceMetrics(name, mac, device));
        }
    }

    private void OnData(SocketIOResponse response)
    {
        if (_logger.IsEnabled(LogEventLevel.Debug))
        {
            _logger.Debug("Ambient Weather: data received, {Data}", response.GetValue());
        }
        else if (_logger.IsEnabled(LogEventLevel.Information))
        {
            _logger.Information("Ambient Weather: data received");
        }

        var device = response.GetValue<Device>();

        if (device.MacAddress is { } mac)
        {
            var name = _deviceNames[mac];
            _channelWriter.TryWrite(new DeviceMetrics(name, mac, device));
        }
    }

    public void Dispose()
    {
        if (_logger != null!)
        {
            _logger.Debug("Disposing {Type}", typeof(AmbientWeatherRealtimeClient).FullName);
        }

        if (_client != null!)
        {
            _client.Off("subscribed");
            _client.Off("data");

            _client.OnConnected -= _onConnectedHandler;
            _client.OnDisconnected -= _onDisconnectedHandler;

            _client.Dispose();
        }
    }
}
