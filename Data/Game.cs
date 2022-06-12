using System;

namespace AcTools.ServerPlugin.DynamicConditions.Data {
    public class Game {
        public enum SessionType : byte {
            Booking = 0,
            Practice = 1,
            Qualification = 2,
            Race = 3,
            Hotlap = 4,
            TimeAttack = 5,
            Drift = 6,
            Drag = 7
        }

        public class ConditionProperties {
            public static double GetRoadTemperature(double seconds, double ambientTemperature, double weatherCoefficient = 1.0) {
                if (seconds < CommonAcConsts.TimeMinimum || seconds > CommonAcConsts.TimeMaximum) {
                    var minTemperature = GetRoadTemperature(CommonAcConsts.TimeMinimum, ambientTemperature, weatherCoefficient);
                    var maxTemperature = GetRoadTemperature(CommonAcConsts.TimeMaximum, ambientTemperature, weatherCoefficient);
                    var minValue = CommonAcConsts.TimeMinimum;
                    var maxValue = CommonAcConsts.TimeMaximum - 24 * 60 * 60;
                    if (seconds > CommonAcConsts.TimeMaximum) {
                        seconds -= 24 * 60 * 60;
                    }

                    return minTemperature + (maxTemperature - minTemperature) * (seconds - minValue) / (maxValue - minValue);
                }

                var time = (seconds / 60d / 60d - 7d) * 0.04167;
                return ambientTemperature * (1d + 5.33332 * (weatherCoefficient == 0d ? 1d : weatherCoefficient) * (1d - time) *
                        (Math.Exp(-6d * time) * Math.Sin(6d * time) + 0.25) * Math.Sin(0.9 * time));
            }
        }
    }
}