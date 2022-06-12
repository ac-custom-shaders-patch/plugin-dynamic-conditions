using System;

namespace AcTools.ServerPlugin.DynamicConditions.Utils {
    public static class DateTimeExtension {
        public static long ToUnixTimestamp(this DateTime d) {
            return (long)(d.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}
