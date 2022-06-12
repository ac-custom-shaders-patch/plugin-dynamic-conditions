﻿using System.IO;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Kunos;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.Messages {
    public class MsgError : PluginMessage {
        public string ErrorMessage { get; private set; }

        public MsgError()
                : base(ACSProtocol.MessageType.ACSP_ERROR) { }

        protected internal override void Deserialize(BinaryReader br) {
            ErrorMessage = ReadStringW(br);
        }

        protected internal override void Serialize(BinaryWriter bw) {
            bw.Write(ErrorMessage);
        }
    }
}