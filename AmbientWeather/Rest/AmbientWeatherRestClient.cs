using System.Text.Json;

namespace AmbientWeatherToDatadog.AmbientWeather.Rest;

// https://rt.ambientweather.net/v1/devices?applicationKey=&apiKey=
// https://rt.ambientweather.net/v1/devices/macAddress?apiKey=&applicationKey=&endDate=&limit=288

public class AmbientWeatherRestClient
{
    private readonly HttpClient _client = new();

    public async Task<List<Device>?> GetDevices(string applicationKey, string apiKey, CancellationToken cancellationToken)
    {
        var devicesJson = await _client.GetStringAsync($"https://rt.ambientweather.net/v1/devices?applicationKey={applicationKey}&apiKey={apiKey}", cancellationToken);
        return JsonSerializer.Deserialize<List<Device>>(devicesJson);
    }

    public async Task<List<Data>?> GetDeviceData(string applicationKey, string apiKey, string deviceMacAddress, int limit, CancellationToken cancellationToken)
    {
        var dataJson = await _client.GetStringAsync($"https://rt.ambientweather.net/v1/devices/{deviceMacAddress}?applicationKey={applicationKey}&apiKey={apiKey}&limit={limit}", cancellationToken);
        return JsonSerializer.Deserialize<List<Data>>(dataJson);
    }
}
