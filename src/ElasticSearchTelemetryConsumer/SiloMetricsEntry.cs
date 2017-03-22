using System;

namespace Orleans.Telemetry
{
    [Serializable]
    internal class SiloMetricsEntry
    {
        public string SiloId { get; set; }
        public string SiloName { get; set; }
        public string DeploymentId { get; set; }
        public string Address { get; set; }
        public string HostName { get; set; }
        public string GatewayAddress { get; set; }
        public double CpuUsage { get; set; }
        public long TotalPhysicalMemory { get; set; }
        public long AvailablePhysicalMemory { get; set; }        
        public long MemoryUsage { get; set; }
        public int SendQueueLength { get; set; }
        public int ReceiveQueueLength { get; set; }
        public long SentMessages { get; set; }
        public long ReceivedMessages { get; set; }
        public int ActivationsCount { get; set; }
        public int RecentlyUsedActivations { get; set; }
        public int RequestQueueLength { get; set; }
        public bool IsOverloaded { get; set; }
        public long ClientCount { get; set; }
        public string Time { get; set; }
        public DateTime UtcDateTime { get; set; }

        public override string ToString() => $"SiloMetricsEntry[ SiloId={SiloId} DeploymentId={DeploymentId} Address={Address} HostName={HostName} GatewayAddress={GatewayAddress} CpuUsage={CpuUsage} TotalPhysicalMemory={TotalPhysicalMemory} AvailablePhysicalMemory={AvailablePhysicalMemory} MemoryUsage={MemoryUsage} SendQueueLength={SendQueueLength} ReceiveQueueLength={ReceiveQueueLength} SentMessages={SentMessages} ReceivedMessages={ReceivedMessages} ActivationsCount={ActivationsCount} RecentlyUsedActivations={RecentlyUsedActivations} RequestQueueLength={RequestQueueLength} IsOverloaded={IsOverloaded} ClientCount={ClientCount} Time={Time} ]";
    }
}