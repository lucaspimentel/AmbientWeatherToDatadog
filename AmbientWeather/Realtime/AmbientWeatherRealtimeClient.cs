using System.Threading.Channels;
using Serilog;
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

    public AmbientWeatherRealtimeClient(ChannelWriter<DeviceMetrics> channelWriter, ILogger logger, string applicationKey, string apiKey)
    {
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

        client.On("subscribed", OnSubscribed);
        client.On("data", OnData);
        client.OnConnected += OnConnected;
        client.OnDisconnected += OnDisconnected;

        _client = client;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        const string connectCommand = "connect";
        const string subscribeCommand = "subscribe";

        _logger.Information("Connecting to Ambient Weather server at {Url}", BaseUrl);

        await _client.ConnectAsync();

        _logger.Information("Sending {Command} command", connectCommand);

        await _client.EmitAsync(
            connectCommand,
            cancellationToken,
            response => _logger.Information("Response: {Response}", response));

        _logger.Information("Sending {Command} command", subscribeCommand);

        await _client.EmitAsync(
            subscribeCommand,
            cancellationToken,
            response => _logger.Information("Response: {Response}", response),
            new ApiKeys(_apiKey));
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        const string disconnectCommand = "disconnect";
        const string unsubscribeCommand = "unsubscribe";

        _client.Off("subscribed");
        _client.Off("data");

        _logger.Information("Sending {Command} command", unsubscribeCommand);

        await _client.EmitAsync(
            unsubscribeCommand,
            cancellationToken,
            response => _logger.Information("Response: {Response}", response),
            new ApiKeys(_apiKey));

        _logger.Information("Sending {Command} command", disconnectCommand);

        await _client.EmitAsync(
            disconnectCommand,
            cancellationToken,
            response => _logger.Information("Response: {Response}", response));

        _logger.Information("Disconnecting from Ambient Weather server");

        await _client.DisconnectAsync();
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        _logger.Information("Connected");
    }

    private void OnDisconnected(object? sender, string e)
    {
        _logger.Information("Disconnected");
    }

    private void OnSubscribed(SocketIOResponse response)
    {
        var value = response.GetValue<Root>();
        var userDevice = value.Devices?.FirstOrDefault();

        _logger.Information("Subscription change: {Method} {Name} ({Mac})", value.Method, userDevice?.Info?.Name, userDevice?.MacAddress);
        _logger.Information("Subscription data: {Data}", response.GetValue());

        if (userDevice is { MacAddress: { } mac, Info.Name: { } name, LastData: { } device })
        {
            _deviceNames[mac] = name;
            _channelWriter.TryWrite(new DeviceMetrics(name, mac, device));
        }
    }

    private void OnData(SocketIOResponse response)
    {
        _logger.Information("Data received: {Data}", response.GetValue());

        var device = response.GetValue<Device>();

        if (device.MacAddress is { } mac)
        {
            var name = _deviceNames[mac];
            _channelWriter.TryWrite(new DeviceMetrics(name, mac, device));
        }
    }

    public void Dispose()
    {
        if (_client != null!)
        {
            _client.Off("subscribed");
            _client.Off("data");
            _client.OnConnected -= OnConnected;
            _client.OnDisconnected -= OnDisconnected;

            _client.Dispose();
        }
    }
}
