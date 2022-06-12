using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins.CspCommands;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Messages;
using AcTools.ServerPlugin.DynamicConditions.Data;
using AcTools.ServerPlugin.DynamicConditions.Utils;
using JetBrains.Annotations;
using SystemHalf;

namespace AcTools.ServerPlugin.DynamicConditions {
    public class LiveConditionParams {
        public bool UseV2;
        public string ApiKey;

        public double TrackLatitude;
        public double TrackLongitude;
        public double TrackLengthKm;
        public string TimezoneId;

        public bool UseRealConditions;
        public TimeSpan TimeOffset;
        public bool UseFixedStartingTime;
        public int FixedStartingTimeValue = 12 * 60 * 60;
        public DateTime FixedStartingDateValue = DateTime.Now;
        public double TimeMultiplier = 1d;
        public double TemperatureOffset;
        public bool UseFixedAirTemperature;
        public double FixedAirTemperature = 25d;
        public TimeSpan WeatherTypeChangePeriod = TimeSpan.FromMinutes(5d);
        public bool WeatherTypeChangeToNeighboursOnly = true;
        public double WeatherRainChance = 0.05;
        public double WeatherThunderChance = 0.005;
        public double TrackGripStartingValue = 99d;
        public double TrackGripIncreasePerLap = 0.05d;
        public double TrackGripTransfer = 80d;

        public double RainTimeMultiplier = 1d;
        public TimeSpan RainWetnessIncreaseTime = TimeSpan.FromMinutes(3d);
        public TimeSpan RainWetnessDecreaseTime = TimeSpan.FromMinutes(15d);
        public TimeSpan RainWaterIncreaseTime = TimeSpan.FromMinutes(30d);
        public TimeSpan RainWaterDecreaseTime = TimeSpan.FromMinutes(120d);
    }

    public class LiveConditionsServerPlugin : AcServerPlugin {
        private static readonly TimeSpan UpdateRainPeriod = TimeSpan.FromSeconds(0.5d);
        private static readonly TimeSpan UpdateWeatherPeriod = TimeSpan.FromMinutes(10d);

        private readonly LiveConditionParams _liveParams;
        private TimeSpan _updatePeriod;
        private TimeSpan _broadcastPeriod;
        private double _drivenLapsEstimate;
        private double _lapLengthKm;
        private bool _disposed;

        public LiveConditionsServerPlugin(LiveConditionParams liveParams) {
            _liveParams = liveParams;
            _lapLengthKm = Math.Max(1, liveParams.TrackLengthKm);
            _startingDate = DateTime.Now;
            _broadcastPeriod = liveParams.UseRealConditions ? TimeSpan.FromMinutes(1d)
                    : TimeSpan.FromMinutes((liveParams.WeatherTypeChangePeriod.TotalMinutes * 0.2).Clamp(0.1, 2d));
            _startingDate = _liveParams.UseFixedStartingTime
                    ? _liveParams.FixedStartingDateValue.Date + TimeSpan.FromSeconds(_liveParams.FixedStartingTimeValue)
                            - GetTimezoneOffset()
                    : DateTime.Now + _liveParams.TimeOffset;
            SyncWeatherAsync().Ignore();
            UpdateStateAsync().Ignore();
        }

        private static void LogMessage(string msg) {
            Logging.Write(msg);
        }

        private TimeSpan GetTimezoneOffset() {
            var utcOffset = new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero);
            return TimeZoneInfo.FindSystemTimeZoneById(_liveParams.TimezoneId).GetUtcOffset(utcOffset) - TimeZoneInfo.Local.GetUtcOffset(utcOffset);
        }

        public override void OnInit() {
            base.OnInit();
            BroadcastLoopAsync().Ignore();
        }

        public override void Dispose() {
            _disposed = true;
        }

        public override void OnNewSession(MsgSessionInfo msg) {
            base.OnNewSession(msg);
            _drivenLapsEstimate *= (_liveParams.TrackGripTransfer / 100d).Saturate();
        }

        private string GetSerializedCspCommand([NotNull] WeatherDescription current, [NotNull] WeatherDescription next) {
            var transition = GetWeatherTransition();
            var date = _startingDate + TimeSpan.FromSeconds(_timePassedTotal.Elapsed.TotalSeconds * _liveParams.TimeMultiplier);
            var ambientTemperature = _liveParams.UseFixedAirTemperature
                    ? _liveParams.FixedAirTemperature : transition.Lerp(current.Temperature, next.Temperature) + _liveParams.TemperatureOffset;
            var roadTemperature = Game.ConditionProperties.GetRoadTemperature(date.TimeOfDay.TotalSeconds.RoundToInt(), ambientTemperature,
                    transition.Lerp(GetRoadTemperatureCoefficient(current.Type), GetRoadTemperatureCoefficient(next.Type)));
            var windSpeed = transition.Lerp(current.WindSpeed, next.WindSpeed) * 3.6;
            var grip = ((_liveParams.TrackGripStartingValue + _liveParams.TrackGripIncreasePerLap * _drivenLapsEstimate) / 100d).Clamp(0.6, 1d);

            // Second version would affect conditions physics, so it’s optional, active only if server requires CSP v1643 or newer
            return _liveParams.UseV2
                    ? new CommandWeatherSetV2 {
                        Timestamp = (ulong)date.ToUnixTimestamp(),
                        TimeToApply = (Half)_broadcastPeriod.TotalSeconds,
                        WeatherCurrent = (CommandWeatherType)current.Type,
                        WeatherNext = (CommandWeatherType)next.Type,
                        Transition = (ushort)(65535 * transition),
                        WindSpeedKmh = (Half)windSpeed,
                        WindDirectionDeg = (Half)transition.Lerp(current.WindDirection, next.WindDirection),
                        Humidity = (byte)(transition.Lerp(current.Humidity, next.Humidity) * 255d),
                        Pressure = (Half)transition.Lerp(current.Pressure, next.Pressure),
                        TemperatureAmbient = (Half)ambientTemperature,
                        TemperatureRoad = (Half)roadTemperature,
                        TrackGrip = grip,
                        RainIntensity = (Half)_rainIntensity,
                        RainWetness = (Half)_rainWetness,
                        RainWater = (Half)_rainLag[_rainCursor]
                    }.Serialize()
                    : new CommandWeatherSetV1 {
                        Timestamp = (ulong)date.ToUnixTimestamp(),
                        TimeToApply = (float)_broadcastPeriod.TotalSeconds,
                        WeatherCurrent = (CommandWeatherType)current.Type,
                        WeatherNext = (CommandWeatherType)next.Type,
                        Transition = (float)transition,
                    }.Serialize();
        }

        public override void OnClientLoaded(MsgClientLoaded msg) {
            // Sending conditions directly to all newly connected:
            if (_weatherCurrent != null && _weatherNext != null) {
                PluginManager.SendCspCommand(msg.CarId, GetSerializedCspCommand(_weatherCurrent, _weatherNext));
            }
        }

        private async Task BroadcastLoopAsync() {
            while (!_disposed) {
                if (_weatherCurrent != null && _weatherNext != null && PluginManager.IsConnected) {
                    PluginManager.BroadcastCspCommand(GetSerializedCspCommand(_weatherCurrent, _weatherNext));
                }
                await Task.Delay(_broadcastPeriod);
            }
        }

        private readonly Stopwatch _timePassedTotal = Stopwatch.StartNew();
        private readonly Stopwatch _weatherTransitionStopwatch = Stopwatch.StartNew();

        [CanBeNull]
        private WeatherDescription _weatherCurrent, _weatherNext;

        private double _rainCurrent, _rainNext;

        private DateTime _startingDate;
        private double _rainIntensity;
        private double _rainWetness;
        private double _rainWater;

        private int _rainCursor;
        private double[] _rainLag = new double[120]; // two updates per second → 1 minute for changes to apply

        private double GetWeatherTransition() {
            return (_weatherTransitionStopwatch.Elapsed.TotalSeconds / _updatePeriod.TotalSeconds).Saturate().SmoothStep();
        }

        private async Task UpdateStateAsync() {
            while (!_disposed) {
                var transition = GetWeatherTransition();

                var current = _weatherCurrent;
                var next = _weatherNext;
                var weatherMissing = current == null || next == null;
                _rainIntensity = weatherMissing ? 0d : transition.Lerp(_rainCurrent, _rainNext);

                var date = _startingDate + TimeSpan.FromSeconds(_timePassedTotal.Elapsed.TotalSeconds * _liveParams.TimeMultiplier);
                var ambientTemperature = weatherMissing ? 25d : _liveParams.UseFixedAirTemperature
                        ? _liveParams.FixedAirTemperature : transition.Lerp(current.Temperature, next.Temperature) + _liveParams.TemperatureOffset;
                var roadTemperature = Game.ConditionProperties.GetRoadTemperature(date.TimeOfDay.TotalSeconds.RoundToInt(), ambientTemperature,
                        transition.Lerp(
                                GetRoadTemperatureCoefficient(current?.Type ?? WeatherType.Clear),
                                GetRoadTemperatureCoefficient(next?.Type ?? WeatherType.Clear)));

                if (_rainIntensity > 0d) {
                    _rainWetness = Math.Min(1d, _rainWetness + _rainIntensity.Lerp(0.3, 1.7d)
                            * UpdateRainPeriod.TotalSeconds / Math.Max(1d, _liveParams.RainWetnessIncreaseTime.TotalSeconds * _liveParams.RainTimeMultiplier));
                } else {
                    _rainWetness = Math.Max(0d, _rainWetness - roadTemperature.LerpInvSat(10d, 35d).Lerp(0.3, 1.7d)
                            * UpdateRainPeriod.TotalSeconds / Math.Max(1d, _liveParams.RainWetnessDecreaseTime.TotalSeconds * _liveParams.RainTimeMultiplier));
                }

                if (_rainWater < _rainIntensity) {
                    _rainWater = Math.Min(_rainIntensity, _rainWater + _rainIntensity.Lerp(0.3, 1.7d)
                            * UpdateRainPeriod.TotalSeconds / Math.Max(1d, _liveParams.RainWaterIncreaseTime.TotalSeconds * _liveParams.RainTimeMultiplier));
                } else {
                    _rainWater = Math.Max(_rainIntensity, _rainWater - roadTemperature.LerpInvSat(10d, 35d).Lerp(0.3, 1.7d)
                            * UpdateRainPeriod.TotalSeconds / Math.Max(1d, _liveParams.RainWaterDecreaseTime.TotalSeconds * _liveParams.RainTimeMultiplier));
                }

                if (PluginManager != null) {
                    foreach (var info in PluginManager.GetDriverInfos()) {
                        if (info?.IsConnected == true) {
                            var drivenDistanceKm = info.CurrentSpeed * UpdateRainPeriod.TotalHours;
                            _drivenLapsEstimate += drivenDistanceKm / _lapLengthKm;
                        }
                    }
                }

                _rainLag[_rainCursor] = _rainWater;
                if (++_rainCursor == _rainLag.Length) {
                    _rainCursor = 0;
                }

                await Task.Delay(UpdateRainPeriod);
            }
        }

        private void ApplyWeather([CanBeNull] WeatherDescription weather) {
            LogMessage("New weather type: " + weather?.Type);
            if (weather == null) {
                return;
            }

            var rain = GetRainIntensity(weather.Type) * MathUtils.Random(0.8, 1.2);
            _weatherTransitionStopwatch.Restart();
            _rainCurrent = _weatherNext != null ? _rainNext : rain;
            _rainNext = rain;
            _weatherCurrent = _weatherNext ?? weather;
            _weatherNext = weather;
        }

        private static readonly Dictionary<WeatherType, List<WeatherType>> Neighbours = new Dictionary<WeatherType, List<WeatherType>>();

        static LiveConditionsServerPlugin() {
            void AddConnection(WeatherType a, WeatherType b) {
                if (!Neighbours.ContainsKey(a)) Neighbours[a] = new List<WeatherType>();
                if (!Neighbours.ContainsKey(b)) Neighbours[b] = new List<WeatherType>();
                Neighbours[a].Add(b);
                Neighbours[b].Add(a);
            }

            AddConnection(WeatherType.Clear, WeatherType.FewClouds);
            AddConnection(WeatherType.Clear, WeatherType.ScatteredClouds);
            AddConnection(WeatherType.Clear, WeatherType.Mist);
            AddConnection(WeatherType.FewClouds, WeatherType.ScatteredClouds);
            AddConnection(WeatherType.FewClouds, WeatherType.BrokenClouds);
            AddConnection(WeatherType.FewClouds, WeatherType.Mist);
            AddConnection(WeatherType.FewClouds, WeatherType.Windy);
            AddConnection(WeatherType.FewClouds, WeatherType.LightDrizzle);
            AddConnection(WeatherType.ScatteredClouds, WeatherType.BrokenClouds);
            AddConnection(WeatherType.ScatteredClouds, WeatherType.OvercastClouds);
            AddConnection(WeatherType.ScatteredClouds, WeatherType.Mist);
            AddConnection(WeatherType.ScatteredClouds, WeatherType.Windy);
            AddConnection(WeatherType.ScatteredClouds, WeatherType.Drizzle);
            AddConnection(WeatherType.ScatteredClouds, WeatherType.LightRain);
            AddConnection(WeatherType.BrokenClouds, WeatherType.OvercastClouds);
            AddConnection(WeatherType.BrokenClouds, WeatherType.Mist);
            AddConnection(WeatherType.BrokenClouds, WeatherType.Windy);
            AddConnection(WeatherType.BrokenClouds, WeatherType.Drizzle);
            AddConnection(WeatherType.BrokenClouds, WeatherType.Rain);
            AddConnection(WeatherType.OvercastClouds, WeatherType.Mist);
            AddConnection(WeatherType.OvercastClouds, WeatherType.Fog);
            AddConnection(WeatherType.OvercastClouds, WeatherType.Windy);
            AddConnection(WeatherType.OvercastClouds, WeatherType.HeavyDrizzle);
            AddConnection(WeatherType.OvercastClouds, WeatherType.Rain);
            AddConnection(WeatherType.Fog, WeatherType.Mist);
            AddConnection(WeatherType.Fog, WeatherType.Rain);
            AddConnection(WeatherType.Mist, WeatherType.LightDrizzle);
            AddConnection(WeatherType.Mist, WeatherType.LightRain);
            AddConnection(WeatherType.Mist, WeatherType.Windy);
            AddConnection(WeatherType.Windy, WeatherType.Drizzle);
            AddConnection(WeatherType.Windy, WeatherType.LightRain);
            AddConnection(WeatherType.Windy, WeatherType.Rain);
            AddConnection(WeatherType.LightDrizzle, WeatherType.Drizzle);
            AddConnection(WeatherType.LightDrizzle, WeatherType.HeavyDrizzle);
            AddConnection(WeatherType.LightDrizzle, WeatherType.LightRain);
            AddConnection(WeatherType.LightDrizzle, WeatherType.Rain);
            AddConnection(WeatherType.Drizzle, WeatherType.HeavyDrizzle);
            AddConnection(WeatherType.Drizzle, WeatherType.LightRain);
            AddConnection(WeatherType.Drizzle, WeatherType.Rain);
            AddConnection(WeatherType.HeavyDrizzle, WeatherType.LightRain);
            AddConnection(WeatherType.HeavyDrizzle, WeatherType.Rain);
            AddConnection(WeatherType.HeavyDrizzle, WeatherType.HeavyRain);
            AddConnection(WeatherType.HeavyDrizzle, WeatherType.LightThunderstorm);
            AddConnection(WeatherType.LightRain, WeatherType.Rain);
            AddConnection(WeatherType.LightRain, WeatherType.LightThunderstorm);
            AddConnection(WeatherType.Rain, WeatherType.HeavyRain);
            AddConnection(WeatherType.Rain, WeatherType.Thunderstorm);
            AddConnection(WeatherType.HeavyRain, WeatherType.Thunderstorm);
            AddConnection(WeatherType.HeavyRain, WeatherType.HeavyThunderstorm);
            AddConnection(WeatherType.LightThunderstorm, WeatherType.Thunderstorm);
            AddConnection(WeatherType.Thunderstorm, WeatherType.HeavyThunderstorm);

            foreach (var neighbour in Neighbours) {
                neighbour.Value.Add(neighbour.Key);
            }
        }

        private static readonly List<Tuple<double, WeatherType>> ChancesRegular = new List<Tuple<double, WeatherType>> {
            Tuple.Create(0.4, WeatherType.Clear),
            Tuple.Create(0.2, WeatherType.FewClouds),
            Tuple.Create(0.1, WeatherType.ScatteredClouds),
            Tuple.Create(0.1, WeatherType.BrokenClouds),
            Tuple.Create(0.2, WeatherType.OvercastClouds),
            Tuple.Create(0.1, WeatherType.Fog),
            Tuple.Create(0.1, WeatherType.Mist),
            Tuple.Create(0.2, WeatherType.Windy),
        };

        private static readonly List<Tuple<double, WeatherType>> ChancesRain = new List<Tuple<double, WeatherType>> {
            Tuple.Create(0.1, WeatherType.LightDrizzle),
            Tuple.Create(0.2, WeatherType.Drizzle),
            Tuple.Create(0.2, WeatherType.HeavyDrizzle),
            Tuple.Create(0.2, WeatherType.LightRain),
            Tuple.Create(0.3, WeatherType.Rain),
            Tuple.Create(0.1, WeatherType.HeavyRain),
        };

        private static readonly List<Tuple<double, WeatherType>> ChancesThunderstorm = new List<Tuple<double, WeatherType>> {
            Tuple.Create(0.6, WeatherType.LightThunderstorm),
            Tuple.Create(0.3, WeatherType.Thunderstorm),
            Tuple.Create(0.1, WeatherType.HeavyThunderstorm),
        };

        private WeatherType? PickRandomWeatherType(List<Tuple<double, WeatherType>> table, [CanBeNull] List<WeatherType> allowed) {
            if (allowed != null) {
                table = table.Where(x => allowed.Contains(x.Item2)).ToList();
            }
            if (table.Count == 0) return null;
            var chance = MathUtils.Random() * table.Sum(x => x.Item1);
            foreach (var item in table) {
                if (chance < item.Item1) return item.Item2;
                chance -= item.Item1;
            }
            return table.FirstOrDefault()?.Item2;
        }

        private WeatherType GenerateRandomWeatherType(WeatherType? currentType) {
            if (!_liveParams.WeatherTypeChangeToNeighboursOnly && currentType.HasValue && MathUtils.Random() < 0.2) {
                return currentType.Value;
            }

            List<WeatherType> allowed = null;
            if (currentType.HasValue && _liveParams.WeatherTypeChangeToNeighboursOnly) {
                Neighbours.TryGetValue(currentType.Value, out allowed);
            }

            var chance = MathUtils.Random() * Math.Max(1, _liveParams.WeatherThunderChance + _liveParams.WeatherRainChance) - _liveParams.WeatherThunderChance;
            if (chance < 0 || MathUtils.Random() > 0.2 && ChancesThunderstorm.Any(x => x.Item2 == currentType)) {
                var type = PickRandomWeatherType(ChancesThunderstorm, allowed);
                if (type.HasValue) {
                    return type.Value;
                }
            }
            if (chance < _liveParams.WeatherRainChance || MathUtils.Random() > 0.2 && ChancesRain.Any(x => x.Item2 == currentType)) {
                var type = PickRandomWeatherType(ChancesRain, allowed);
                if (type.HasValue) {
                    return type.Value;
                }
            }
            return PickRandomWeatherType(ChancesRegular, allowed) ?? WeatherType.Clear;
        }

        private Tuple<double, double, double> EstimateTemperatureWindSpeedHumidity(WeatherType type) {
            if (type == WeatherType.Clear) return Tuple.Create(26d, 1d, 0.6);
            if (type == WeatherType.FewClouds) return Tuple.Create(25d, 2d, 0.6);
            if (type == WeatherType.ScatteredClouds) return Tuple.Create(25d, 3d, 0.6);
            if (type == WeatherType.BrokenClouds) return Tuple.Create(25d, 4d, 0.6);
            if (type == WeatherType.OvercastClouds) return Tuple.Create(24d, 5d, 0.6);
            if (type == WeatherType.Fog) return Tuple.Create(24d, 0d, 0.9);
            if (type == WeatherType.Mist) return Tuple.Create(24d, 0d, 0.8);
            if (type == WeatherType.Windy) return Tuple.Create(24d, 10d, 0.4);
            if (type == WeatherType.LightDrizzle) return Tuple.Create(25d, 2d, 0.7);
            if (type == WeatherType.Drizzle) return Tuple.Create(24d, 3d, 0.7);
            if (type == WeatherType.HeavyDrizzle) return Tuple.Create(23d, 4d, 0.7);
            if (type == WeatherType.LightRain) return Tuple.Create(24d, 4d, 0.8);
            if (type == WeatherType.Rain) return Tuple.Create(23d, 6d, 0.8);
            if (type == WeatherType.HeavyRain) return Tuple.Create(23d, 10d, 0.8);
            if (type == WeatherType.LightThunderstorm) return Tuple.Create(23d, 12d, 0.9);
            if (type == WeatherType.Thunderstorm) return Tuple.Create(22d, 13d, 0.9);
            if (type == WeatherType.HeavyThunderstorm) return Tuple.Create(22d, 14d, 0.9);
            return Tuple.Create(24d, 1d, 0.6);
        }

        private WeatherDescription GenerateRandomDescription(WeatherType? currentType) {
            var type = GenerateRandomWeatherType(currentType);
            Logging.Debug($"Switching from {currentType?.ToString() ?? "?"} to {type}");
            var estimated = EstimateTemperatureWindSpeedHumidity(type);
            return new WeatherDescription(type, estimated.Item1 * MathUtils.Random(0.95, 1.05), estimated.Item2 * MathUtils.Random(0.6, 1.2),
                    MathUtils.Random() * 360, estimated.Item3 * MathUtils.Random(0.6, 1.2), 1030);
        }

        private async Task SyncWeatherAsync() {
            if (_liveParams.UseRealConditions) {
                _updatePeriod = UpdateWeatherPeriod;
                while (!_disposed) {
                    ApplyWeather(await OpenWeatherApiProvider.GetWeatherAsync(_liveParams.ApiKey, _liveParams.TrackLatitude, _liveParams.TrackLongitude));
                    await Task.Delay(UpdateWeatherPeriod);
                }
            } else {
                while (!_disposed) {
                    _updatePeriod = TimeSpan.FromSeconds(_liveParams.WeatherTypeChangePeriod.TotalSeconds * MathUtils.Random(0.5, 1.5));
                    ApplyWeather(GenerateRandomDescription(_weatherNext?.Type));
                    await Task.Delay(_updatePeriod);
                }
            }
        }

        private static double GetRainIntensity(WeatherType type) {
            switch (type) {
                case WeatherType.None:
                    return 0d;
                case WeatherType.LightThunderstorm:
                    return 0.5;
                case WeatherType.Thunderstorm:
                    return 0.6;
                case WeatherType.HeavyThunderstorm:
                    return 0.7;
                case WeatherType.LightDrizzle:
                    return 0.1;
                case WeatherType.Drizzle:
                    return 0.2;
                case WeatherType.HeavyDrizzle:
                    return 0.3;
                case WeatherType.LightRain:
                    return 0.3;
                case WeatherType.Rain:
                    return 0.4;
                case WeatherType.HeavyRain:
                    return 0.5;
                case WeatherType.LightSnow:
                    return 0.2;
                case WeatherType.Snow:
                    return 0.3;
                case WeatherType.HeavySnow:
                    return 0.4;
                case WeatherType.LightSleet:
                    return 0.3;
                case WeatherType.Sleet:
                    return 0.4;
                case WeatherType.HeavySleet:
                    return 0.5;
                case WeatherType.Squalls:
                    return 0.6;
                case WeatherType.Tornado:
                    return 0.5;
                case WeatherType.Hurricane:
                    return 0.6;
                default:
                    return 0d;
            }
        }

        private static double GetRoadTemperatureCoefficient(WeatherType type) {
            switch (type) {
                case WeatherType.None:
                    return 1d;
                case WeatherType.LightThunderstorm:
                    return 0.7;
                case WeatherType.Thunderstorm:
                    return 0.2;
                case WeatherType.HeavyThunderstorm:
                    return -0.2;
                case WeatherType.LightDrizzle:
                    return 0.1;
                case WeatherType.Drizzle:
                    return -0.1;
                case WeatherType.HeavyDrizzle:
                    return -0.3;
                case WeatherType.LightRain:
                    return 0.01;
                case WeatherType.Rain:
                    return -0.2;
                case WeatherType.HeavyRain:
                    return -0.5;
                case WeatherType.LightSnow:
                    return -0.7;
                case WeatherType.Snow:
                    return -0.8;
                case WeatherType.HeavySnow:
                    return -0.9;
                case WeatherType.LightSleet:
                    return -1d;
                case WeatherType.Sleet:
                    return -1d;
                case WeatherType.HeavySleet:
                    return -1d;
                case WeatherType.Squalls:
                    return -0.5;
                case WeatherType.Tornado:
                    return -0.3;
                case WeatherType.Hurricane:
                    return -0.6;
                case WeatherType.Clear:
                    return 1d;
                case WeatherType.FewClouds:
                    return 1d;
                case WeatherType.ScatteredClouds:
                    return 0.8;
                case WeatherType.BrokenClouds:
                    return 0.1;
                case WeatherType.OvercastClouds:
                    return 0.01;
                case WeatherType.Fog:
                    return -0.3;
                case WeatherType.Mist:
                    return -0.2;
                case WeatherType.Smoke:
                    return -0.2;
                case WeatherType.Haze:
                    return 0.9;
                case WeatherType.Sand:
                    return 1d;
                case WeatherType.Dust:
                    return 1d;
                case WeatherType.Cold:
                    return -0.8;
                case WeatherType.Hot:
                    return 1d;
                case WeatherType.Windy:
                    return 0.3;
                case WeatherType.Hail:
                    return -1d;
                default:
                    return 0d;
            }
        }
    }
}