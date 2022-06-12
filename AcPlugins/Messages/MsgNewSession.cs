using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Kunos;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.Messages {
    public class MsgNewSession : MsgSessionInfo {
        public MsgNewSession() {
            Type = ACSProtocol.MessageType.ACSP_NEW_SESSION;
        }
    }
}