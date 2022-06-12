namespace AcTools.ServerPlugin.DynamicConditions.Data {
    public class WeatherDescription {
        public WeatherType Type { get; }

        public double Temperature { get; }

        /// <summary>
        /// Units: m/s.
        /// </summary>
        public double WindSpeed { get; }

        public double WindDirection { get; }

        /// <summary>
        /// Units: percentage.
        /// </summary>
        public double Humidity  { get; }

        /// <summary>
        /// Units: hPa, aka 100 Pa.
        /// </summary>
        public double Pressure  { get; }

        public WeatherDescription(WeatherType type, double temperature, double windSpeed, double windDirection, double humidity, double pressure) {
            Type = type;
            Temperature = temperature;
            WindSpeed = windSpeed;
            WindDirection = windDirection;
            Humidity = humidity;
            Pressure = pressure;
        }
    }
}
