using System;

namespace Orleans.Telemetry
{
    [Serializable]
    internal class StatsTableEntry
    {
        public string Identity { get; set; }        
        public string DeploymentId { get; set; }        
        public string Name { get; set; }        
        public string HostName { get; set; }
        public string Statistic { get; set; }
        public string StatValue { get; set; }
        public bool IsDelta { get; set; }
        public string Time { get; set; }
        public DateTime UtcDateTime { get; set; }

        public override string ToString() => $"StatsTableEntry[ Identity={Identity} DeploymentId={DeploymentId} Name={Name} HostName={HostName} Statistic={Statistic} StatValue={StatValue} IsDelta={IsDelta} Time={Time} ]";
    }
}