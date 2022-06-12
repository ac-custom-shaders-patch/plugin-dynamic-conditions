using System;
using JetBrains.Annotations;

namespace AcTools.ServerPlugin.DynamicConditions.Utils {
    // TODO: Remove MathF
    public static class MathUtils {
        public static double Pow(this double v, double p) => Math.Pow(v, p);
        public static float Pow(this float v, float p) => (float)Math.Pow(v, p);

        public static double Sqrt(this double v) => Math.Sqrt(v);
        public static float Sqrt(this float v) => (float)Math.Sqrt(v);

        public static double Acos(this double v) => Math.Acos(v);
        public static float Acos(this float v) => (float)Math.Acos(v);

        public static double Asin(this double v) => Math.Asin(v);
        public static float Asin(this float v) => (float)Math.Asin(v);

        public static double Sin(this double v) => Math.Sin(v);
        public static float Sin(this float v) => (float)Math.Sin(v);

        public static double Cos(this double v) => Math.Cos(v);
        public static float Cos(this float v) => (float)Math.Cos(v);

        public static double Tan(this double a) => Math.Tan(a);
        public static float Tan(this float a) => (float)Math.Tan(a);

        public static double Atan(this double f) => Math.Atan(f);
        public static float Atan(this float f) => (float)Math.Atan(f);

        public static int Abs(this int v) => v < 0 ? -v : v;
        public static double Abs(this double v) => v < 0d ? -v : v;
        public static float Abs(this float v) => v < 0f ? -v : v;

        public static bool IsFinite(this double v) => !double.IsInfinity(v) && !double.IsNaN(v);
        public static bool IsFinite(this float v) => !float.IsInfinity(v) && !float.IsNaN(v);

        public static int Clamp(this int v, int min, int max) => v < min ? min : v > max ? max : v;
        public static float Clamp(this float v, float min, float max) => v < min ? min : v > max ? max : v;
        public static double Clamp(this double v, double min, double max) => v < min ? min : v > max ? max : v;
        public static TimeSpan Clamp(this TimeSpan v, TimeSpan min, TimeSpan max) => v < min ? min : v > max ? max : v;

        public static byte ClampToByte(this double v) => (byte)(v < 0d ? 0 : v > 255d ? 255 : v);
        public static byte ClampToByte(this int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);

        public static float Saturate(this float value) => value < 0f ? 0f : value > 1f ? 1f : value;
        public static double Saturate(this double value) => value < 0d ? 0d : value > 1d ? 1d : value;

        public static int Sign(this int value) => value < 0 ? -1 : value > 0 ? 1 : 0;
        public static int Sign(this float value) => value < 0f ? -1 : value > 0f ? 1 : 0;
        public static int Sign(this double value) => value < 0d ? -1 : value > 0d ? 1 : 0;

        public static int RoundToInt(this double value) => (int)Math.Round(value);

        [ThreadStatic]
        private static Random _random;

        [NotNull]
        public static Random RandomInstance => _random ?? (_random = new Random(Guid.NewGuid().GetHashCode()));

        public static int Random(int maxValueExclusive) => RandomInstance.Next(maxValueExclusive);
        public static int Random(int minValueInclusive, int maxValueExclusive) => RandomInstance.Next(minValueInclusive, maxValueExclusive);
        public static double Random() => RandomInstance.NextDouble();
        public static double Random(double maxValue) => RandomInstance.NextDouble() * maxValue;
        public static double Random(double minValue, double maxValue) => Random(maxValue - minValue) + minValue;
        public static float Random(float maxValue) => (float)(RandomInstance.NextDouble() * maxValue);
        public static float Random(float minValue, float maxValue) => Random(maxValue - minValue) + minValue;
        public static TimeSpan Random(TimeSpan minValue, TimeSpan maxValue) => TimeSpan.FromSeconds(Random(minValue.TotalSeconds, maxValue.TotalSeconds));

        public static TimeSpan Max(this TimeSpan a, TimeSpan b) => a > b ? a : b;
        public static TimeSpan Min(this TimeSpan a, TimeSpan b) => a < b ? a : b;

        public static float Lerp(this float t, float v0, float v1) => (1f - t) * v0 + t * v1;
        public static double Lerp(this double t, double v0, double v1) => (1d - t) * v0 + t * v1;

        public static float LerpInv(this float t, float v0, float v1) => (t - v0) / (v1 - v0);
        public static double LerpInv(this double t, double v0, double v1) => (t - v0) / (v1 - v0);

        public static float LerpInvSat(this float t, float v0, float v1) => t.LerpInv(v0, v1).Saturate();
        public static double LerpInvSat(this double t, double v0, double v1) => t.LerpInv(v0, v1).Saturate();

        public static float Remap(this float t, float v0, float v1, float o0, float o1) => (t - v0) / (v1 - v0) * (o1 - o0) + o0;
        public static double Remap(this double t, double v0, double v1, double o0, double o1) => (t - v0) / (v1 - v0) * (o1 - o0) + o0;

        /// <summary>
        /// For normalized and saturated X.
        /// </summary>
        public static float SmootherStep(this float x) => x * x * x * (x * (x * 6f - 15f) + 10f);

        /// <summary>
        /// For normalized and saturated X.
        /// </summary>
        public static double SmootherStep(this double x) => x * x * x * (x * (x * 6d - 15d) + 10d);

        public static float SmootherStep(this float x, float edge0, float edge1) => ((x - edge0) / (edge1 - edge0)).Saturate().SmootherStep();
        public static double SmootherStep(this double x, double edge0, double edge1) => ((x - edge0) / (edge1 - edge0)).Saturate().SmootherStep();

        /// <summary>
        /// For normalized and saturated X.
        /// </summary>
        public static float SmoothStep(this float x) => x * x * (3f - 2f * x);

        /// <summary>
        /// For normalized and saturated X.
        /// </summary>
        public static double SmoothStep(this double x) => x * x * (3d - 2d * x);

        public static float SmoothStep(this float x, float edge0, float edge1) => ((x - edge0) / (edge1 - edge0)).Saturate().SmoothStep();
        public static double SmoothStep(this double x, double edge0, double edge1) => ((x - edge0) / (edge1 - edge0)).Saturate().SmoothStep();
    }
}