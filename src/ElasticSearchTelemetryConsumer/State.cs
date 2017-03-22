using System;

namespace Orleans.Telemetry
{
    class State
    {
        public string DeploymentId { get; set; } = "";
        public bool IsSilo { get; set; } = true;
        public string SiloName { get; set; } = "";
        public string Id { get; set; } = "";
        public string Address { get; set; } = "";
        public string GatewayAddress { get; set; } = "";
        public string HostName { get; set; } = "";
        public Guid ServiceId { get; set; } = Guid.Empty;
    }
}