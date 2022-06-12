using System.IO;
using System.Reflection;

namespace AcTools.ServerPlugin.DynamicConditions.Utils {
    internal static class MainExecutingFile {
        private static string _location;
        private static string _directory;

        public static string Location => _location ?? (_location = Assembly.GetEntryAssembly()?.Location ?? "");

        public static string Directory => _directory ?? (_directory = Path.GetDirectoryName(Location));
    }
}
