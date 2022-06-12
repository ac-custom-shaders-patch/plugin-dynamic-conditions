using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins;

namespace AcTools.ServerPlugin.DynamicConditions {
    public class ProgramParams {
        public AcServerPluginManagerSettings Plugin;
        public List<ExternalPluginInfo> ExternalPlugins;
        public LiveConditionParams Weather;

        public static ProgramParams GetParams(string[] data) {
            var ret = new ProgramParams {
                Plugin = new AcServerPluginManagerSettings {
                    LogServerRequests = false
                },
                ExternalPlugins = new List<ExternalPluginInfo>(),
                Weather = new LiveConditionParams()
            };

            var keys = new Dictionary<string, Action<string>> {
                ["message"] = Console.WriteLine,
                ["plugin.listeningPort"] = x => ret.Plugin.ListeningPort = int.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["plugin.remotePort"] = x => ret.Plugin.RemotePort = int.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["plugin.remoteHostName"] = x => ret.Plugin.RemoteHostName = x,
                ["plugin.serverCapacity"] = x => ret.Plugin.Capacity = int.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["plugin.externalPlugin"] = x => {
                    var p = x.Split(',');
                    if (p.Length != 2) throw new Exception("Format for plugin.externalPlugin: <listeningPort>, <remoteHostName>:<remotePort>");
                    var r = p[1].Split(':');
                    if (r.Length != 2) throw new Exception("Format for plugin.externalPlugin: <listeningPort>, <remoteHostName>:<remotePort>");
                    ret.ExternalPlugins.Add(new ExternalPluginInfo(
                            int.Parse(p[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture),
                            r[0].Trim(),
                            int.Parse(r[1], NumberStyles.Any, CultureInfo.InvariantCulture)));
                },
                ["weather.useV2"] = x => ret.Weather.UseV2 = x != "0",
                ["weather.apiKey"] = x => ret.Weather.ApiKey = x,
                ["weather.trackLatitude"] = x => ret.Weather.TrackLatitude = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.trackLongitude"] = x => ret.Weather.TrackLongitude = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.trackLengthKm"] = x => ret.Weather.TrackLengthKm = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.trackTimezoneId"] = x => ret.Weather.TimezoneId = x,
                ["weather.useRealConditions"] = x => ret.Weather.UseRealConditions = x != "0",
                ["weather.timeOffset"] = x => ret.Weather.TimeOffset = TimeSpan.Parse(x, CultureInfo.InvariantCulture),
                ["weather.useFixedStartingTime"] = x => ret.Weather.UseFixedStartingTime = x != "0",
                ["weather.fixedStartingTimeValue"] = x => ret.Weather.FixedStartingTimeValue = int.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.fixedStartingDateValue"] = x => ret.Weather.FixedStartingDateValue = DateTime.Parse(x, CultureInfo.InvariantCulture),
                ["weather.timeMultiplier"] = x => ret.Weather.TimeMultiplier = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.temperatureOffset"] = x => ret.Weather.TemperatureOffset = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.useFixedAirTemperature"] = x => ret.Weather.UseFixedAirTemperature = x != "0",
                ["weather.fixedAirTemperature"] = x => ret.Weather.FixedAirTemperature = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.weatherTypeChangePeriod"] = x => ret.Weather.WeatherTypeChangePeriod = TimeSpan.Parse(x, CultureInfo.InvariantCulture),
                ["weather.weatherTypeChangeToNeighboursOnly"] = x => ret.Weather.WeatherTypeChangeToNeighboursOnly = x != "0",
                ["weather.weatherRainChance"] = x => ret.Weather.WeatherRainChance = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.weatherThunderChance"] = x => ret.Weather.WeatherThunderChance = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.trackGripStartingValue"] = x => ret.Weather.TrackGripStartingValue = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.trackGripIncreasePerLap"] = x => ret.Weather.TrackGripIncreasePerLap = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.trackGripTransfer"] = x => ret.Weather.TrackGripTransfer = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.rainTimeMultiplier"] = x => ret.Weather.RainTimeMultiplier = double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture),
                ["weather.rainWetnessIncreaseTime"] = x => ret.Weather.RainWetnessIncreaseTime = TimeSpan.Parse(x, CultureInfo.InvariantCulture),
                ["weather.rainWetnessDecreaseTime"] = x => ret.Weather.RainWetnessDecreaseTime = TimeSpan.Parse(x, CultureInfo.InvariantCulture),
                ["weather.rainWaterIncreaseTime"] = x => ret.Weather.RainWaterIncreaseTime = TimeSpan.Parse(x, CultureInfo.InvariantCulture),
                ["weather.rainWaterDecreaseTime"] = x => ret.Weather.RainWaterDecreaseTime = TimeSpan.Parse(x, CultureInfo.InvariantCulture),
            };

            foreach (var line in data
                    .Select(x => x.Split('#')[0].Split('=').Select(y => y.Trim()).ToArray())
                    .Where(x => x.Length == 2)) {
                try {
                    if (keys.TryGetValue(line[0], out var fn)) fn(line[1]);
                } catch (Exception e) {
                    throw new Exception($"Failed to parse {line[1]}: {e.Message}");
                }
            }
            return ret;
        }
    }
}