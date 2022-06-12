using System.Runtime.InteropServices;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.CspCommands {
    // This command will be sent back as a response to first command. Format is the same.
    // If you want to kick people with older version or without WeatherFX, add that version and
    // WeatherFX flag to CommandHandshakeIn, but also remember to check CommandHandshakeOut,
    // people might be using older version of CSP or not using it at all.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CommandHandshakeOut : ICspCommand {
        public uint Version;
        public bool IsWeatherFXActive;

        ushort ICspCommand.GetMessageType() {
            return 1;
        }
    };
}