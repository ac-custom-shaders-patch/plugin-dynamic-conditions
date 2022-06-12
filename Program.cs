using System;
using System.IO;
using System.Linq;
using System.Text;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins;
using AcTools.ServerPlugin.DynamicConditions.Utils;

namespace AcTools.ServerPlugin.DynamicConditions {
    internal class Program {
        public static int Main(string[] args) {
            var configs = GetConfigs(args);
            if (configs.Length == 0) {
                Console.Error.WriteLine("Usage: AcTools.ServerPlugin.DynamicConditions.exe <config1.cfg> ...");
                return 1;
            }

            try {
                var managers = configs.Select(arg => {
                    var programParams = ProgramParams.GetParams(arg);
                    var manager = new AcServerPluginManager(programParams.Plugin);
                    foreach (var entry in programParams.ExternalPlugins) {
                        manager.AddExternalPlugin(entry);
                    }

                    manager.AddPlugin(new LiveConditionsServerPlugin(programParams.Weather));
                    manager.Connect();
                    return manager;
                }).ToList();

                Console.WriteLine(managers.Count == 1
                        ? "> Server plugin is running. Press <Enter> to close."
                        : $"> {managers.Count} server plugins are running. Press <Enter> to close.");
                Console.ReadLine();
                return 0;
            } catch (Exception e) {
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
        }

        private static string GetEmbeddedConfig() {
            using (var stream = File.Open(MainExecutingFile.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream, Encoding.Default, true)) {
                reader.BaseStream.Seek(-8, SeekOrigin.End);
                if (reader.ReadUInt32() == 0xBEE5) {
                    var length = reader.ReadInt32();
                    if (length > 0 && length < 1e6) {
                        reader.BaseStream.Seek(-length - 8, SeekOrigin.Current);
                        return Encoding.UTF8.GetString(reader.ReadBytes(length));
                    }
                }
            }
            return null;
        }

        private static string[][] GetConfigs(string[] args) {
            var baked = GetEmbeddedConfig();
            return baked != null ? new[]{ baked.Split('\n') } : args.Select(File.ReadAllLines).ToArray();
        }
    }
}