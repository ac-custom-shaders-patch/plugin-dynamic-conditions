﻿using System.IO;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Kunos;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.Messages {
    public class MsgConnectionClosed : PluginMessage {
        public string DriverName { get; set; }
        public string DriverGuid { get; set; }
        public byte CarId { get; set; }
        public string CarModel { get; set; }
        public string CarSkin { get; set; }

        public MsgConnectionClosed()
                : base(ACSProtocol.MessageType.ACSP_CONNECTION_CLOSED) { }

        protected internal override void Deserialize(BinaryReader br) {
            DriverName = ReadStringW(br);
            DriverGuid = ReadStringW(br);
            CarId = br.ReadByte();
            CarModel = ReadString(br);
            CarSkin = ReadString(br);
        }

        protected internal override void Serialize(BinaryWriter bw) {
            WriteStringW(bw, DriverName);
            WriteStringW(bw, DriverGuid);
            bw.Write(CarId);
            WriteStringW(bw, CarModel);
            WriteStringW(bw, CarSkin);
        }
    }
}