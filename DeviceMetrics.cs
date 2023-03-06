using AmbientWeatherToDatadog.AmbientWeather.Realtime;

namespace AmbientWeatherToDatadog;

public sealed record DeviceMetrics(string DeviceName, string DeviceMac, Device Data);
