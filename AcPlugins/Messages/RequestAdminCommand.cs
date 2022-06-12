using System.IO;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Kunos;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.Messages {
    public class RequestAdminCommand : PluginMessage {
        public string Command { get; set; }

        public RequestAdminCommand()
                : base(ACSProtocol.MessageType.ACSP_ADMIN_COMMAND) { }

        protected internal override void Deserialize(BinaryReader br) {
            Command = ReadStringW(br);
        }

        protected internal override void Serialize(BinaryWriter bw) {
            WriteStringW(bw, Command);
        }
    }
}