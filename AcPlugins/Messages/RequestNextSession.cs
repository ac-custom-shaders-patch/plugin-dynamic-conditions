using System.IO;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Kunos;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.Messages {
    public class RequestNextSession : PluginMessage {
        public RequestNextSession()
                : base(ACSProtocol.MessageType.ACSP_NEXT_SESSION) { }

        protected internal override void Deserialize(BinaryReader br) { }

        protected internal override void Serialize(BinaryWriter bw) { }
    }
}