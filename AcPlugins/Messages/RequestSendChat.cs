﻿using System.IO;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Kunos;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.Messages {
    public class RequestSendChat : PluginMessage {
        public byte CarId { get; set; }
        public string ChatMessage { get; set; }

        public RequestSendChat()
                : base(ACSProtocol.MessageType.ACSP_SEND_CHAT) { }

        protected internal override void Deserialize(BinaryReader br) {
            CarId = br.ReadByte();
            ChatMessage = ReadStringW(br);
        }

        protected internal override void Serialize(BinaryWriter bw) {
            bw.Write(CarId);
            WriteStringW(bw, ChatMessage);
        }
    }
}