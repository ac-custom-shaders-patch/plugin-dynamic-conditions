using System.Runtime.Serialization;
using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Helpers;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.Info {
    [DataContract]
    public class IncidentInfo {
        [DataMember]
        public byte Type { get; set; }

        [DataMember]
        public long Timestamp { get; set; }

        [DataMember]
        public int ConnectionId1 { get; set; }

        [DataMember]
        public int ConnectionId2 { get; set; }

        [DataMember]
        public float ImpactSpeed { get; set; }

        [DataMember]
        public Vector3F WorldPosition { get; set; }

        [DataMember]
        public Vector3F RelPosition { get; set; }
    }
}