using System;
using JetBrains.Annotations;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins {
    public class AcServerPluginManagerSettings {
        /// <summary>
        /// Gets or sets the port on which the plugin manager receives messages from the AC server.
        /// </summary>
        public int ListeningPort { get; set; }

        /// <summary>
        /// Gets or sets the port of the AC server where requests should be send to.
        /// </summary>
        public int RemotePort { get; set; }

        [CanBeNull]
        public string AdminPassword { get; set; }

        public int Capacity { get; set; }

        /// <summary>
        /// Gets or sets the hostname of the AC server.
        /// </summary>
        public string RemoteHostName { get; set; } = "127.0.0.1";

        public TimeSpan RealtimeUpdateInterval { get; set; } = TimeSpan.FromSeconds(0.1);

        public TimeSpan NewSessionStartDelay { get; set; } = TimeSpan.FromSeconds(3d);

        /// <summary>
        /// Gets or sets whether requests to the AC server should be logged.
        /// </summary>
        public bool LogServerRequests { get; set; } = true;

        public bool LogServerErrors { get; set; } = true;

        /// <summary>
        /// Keep alive interval; if this is set to something > 0 the Plugin will
        /// monitor the server and send a ServerTimeout if it's missing
        /// </summary>
        public TimeSpan AcServerKeepAliveInterval { get; set; }
    }
}