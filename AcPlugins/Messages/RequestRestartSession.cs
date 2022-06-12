using System.IO;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Kunos;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.Messages {
    public class RequestRestartSession : PluginMessage {
        public RequestRestartSession()
                : base(ACSProtocol.MessageType.ACSP_RESTART_SESSION) { }

        protected internal override void Deserialize(BinaryReader br) { }

        protected internal override void Serialize(BinaryWriter bw) { }
    }
}