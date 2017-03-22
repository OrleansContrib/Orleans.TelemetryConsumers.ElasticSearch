namespace Orleans.Telemetry
{
    internal class ClientMetricsEntry
    {
        public string Address { get; set; }
        public long AvailablePhysicalMemory { get; set; }
        public string ClientId { get; set; }
        public long ConnectedGatewayCount { get; set; }
        public float CpuUsage { get; set; }
        public string DeploymentId { get; set; }
        public string HostName { get; set; }
        public long MemoryUsage { get; set; }
        public long ReceivedMessages { get; set; }
        public int ReceiveQueueLength { get; set; }
        public int SendQueueLength { get; set; }
        public long SentMessages { get; set; }
        public string Time { get; set; }
        public long TotalPhysicalMemory { get; set; }
    }
}