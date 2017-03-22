using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Providers;
using Nest;
using Orleans.Runtime.Configuration;

namespace Orleans.Telemetry
{
    public class ElasticClientMetricsProvider :
        IConfigurableClientMetricsDataPublisher,
        IClientMetricsDataPublisher,
        IStatisticsPublisher,
                                             IProvider
    {
        private string _elasticHostAddress;
        private string _elasticIndex { get; set; } = "orleans_statistics";
        private string _elasticMetricType { get; set; } = "metric";
        private string _elasticCounterType { get; set; } = "counter";


        private string ElasticHostAddress() => _elasticHostAddress;
        private string ElasticIndex() => _elasticIndex + "-" + DateTime.UtcNow.ToString("yyyy-MM-dd-HH");
        private string ElasticMetricType() => _elasticMetricType;
        private string ElasticCounterType() => _elasticCounterType;


        // Example: 2010-09-02 09:50:43.341 GMT - Variant of UniversalSorta­bleDateTimePat­tern
        const string DATE_TIME_FORMAT = "yyyy-MM-dd-" + "HH:mm:ss.fff 'GMT'";        
        int MAX_BULK_UPDATE_DOCS = 200;
        State _state = new State();
        Logger _logger;   

        /// <summary>
        /// Name of the provider
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Closes provider
        /// </summary>        
        public Task Close() => TaskDone.Done;

        /// <summary>
        /// Initialization of ElasticStatisticsProvider
        /// </summary>        
        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;
            _state.ServiceId = providerRuntime.ServiceId;
            _logger = providerRuntime.GetLogger(typeof(ElasticClientMetricsProvider).Name);

            if (config.Properties.ContainsKey("ElasticHostAddress"))
                _elasticHostAddress = config.Properties["ElasticHostAddress"];

            if (config.Properties.ContainsKey("ElasticIndex"))
                _elasticIndex = config.Properties["ElasticIndex"];

            if (config.Properties.ContainsKey("ElasticMetricType"))
                _elasticMetricType = config.Properties["ElasticMetricType"];
            if (config.Properties.ContainsKey("ElasticCounterype"))
                _elasticCounterType = config.Properties["ElasticCounterType"];

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(_elasticHostAddress))
                throw new ArgumentNullException("ElasticHostAddress");
            if (string.IsNullOrWhiteSpace(_elasticIndex))
                throw new ArgumentNullException("ElasticIndex");
            if (string.IsNullOrWhiteSpace(_elasticMetricType))
                throw new ArgumentNullException("ElasticMetricType");
            if (string.IsNullOrWhiteSpace(_elasticCounterType))
                throw new ArgumentNullException("ElasticCounterType");


            return TaskDone.Done;
        }

        public void AddConfiguration(string deploymentId, string hostName, string clientId, IPAddress address)
        {
            _state.DeploymentId = deploymentId;
            _state.Id = clientId;
            _state.Address = address.ToString();
            _state.HostName = hostName;
        }


        public Task Init(ClientConfiguration config, IPAddress address, string clientId)
        {
            _state.Id = clientId;
            _state.Address = address.ToString();

            return TaskDone.Done;
        }

        public Task Init(bool isSilo, string storageConnectionString, string deploymentId, string address,
            string siloName, string hostName) => TaskDone.Done;



        /// <summary>
        /// Metrics for client
        /// </summary>        
        public async Task ReportMetrics(IClientPerformanceMetrics metricsData)
        {
            if (_logger != null && _logger.IsVerbose3)
                _logger.Verbose3($"{nameof(ElasticClientMetricsProvider)}.ReportMetrics called with metrics: {0}, name: {1}, id: {2}.", metricsData, _state.SiloName, _state.Id);

            try
            {
                var esClient = CreateElasticClient(ElasticHostAddress());

                var clientMetrics = PopulateClientMetricsEntry(metricsData, _state);

                var response = await esClient.IndexAsync(clientMetrics, (ds) => ds.Index(ElasticIndex())
                                                                                .Type(ElasticMetricType()));

                if (!response.IsValid && _logger != null && _logger.IsVerbose)
                    _logger.Verbose(response.ServerError.Status, response.ServerError.Error);
            }
            catch (Exception ex)
            {
                if (_logger != null && _logger.IsVerbose)
                    _logger.Verbose($"{ nameof(ElasticClientMetricsProvider)}.ReportMetrics failed: {0}", ex);

                throw;
            }
        }

        /// <summary>
        /// Stats for Silo and Client
        /// </summary>  
        public async Task ReportStats(List<ICounter> statsCounters)
        {
            if (_logger != null && _logger.IsVerbose3)
                _logger.Verbose3($"{ nameof(ElasticClientMetricsProvider)}.ReportStats called with {0} counters, name: {1}, id: {2}", statsCounters.Count, _state.SiloName, _state.Id);

            try
            {
                var esClient = CreateElasticClient(ElasticHostAddress());

                var counterBatches = statsCounters.Where(cs => cs.Storage == CounterStorage.LogAndTable)
                                                  .OrderBy(cs => cs.Name)
                                                  .Select(cs => PopulateStatsTableEntry(cs, _state))
                                                  .BatchIEnumerable(MAX_BULK_UPDATE_DOCS);

                foreach (var batch in counterBatches)
                {
                    var bulkDesc = new BulkDescriptor();
                    bulkDesc.IndexMany(batch, (bulk, q) => bulk.Index(ElasticIndex())
                                                                .Type(ElasticCounterType()));

                    var response = await esClient.BulkAsync(bulkDesc);

                    if (response.Errors && _logger != null && _logger.IsVerbose)
                        _logger.Error(response.ServerError.Status, response.ServerError.Error);
                }
            }
            catch (Exception ex)
            {
                if (_logger != null && _logger.IsVerbose)
                    _logger.Verbose($"{ nameof(ElasticClientMetricsProvider)}.ReportStats failed: {0}", ex);

                throw;
            }
        }


        static ElasticClient CreateElasticClient(string elasticHostAddress)
        {            
            var node = new Uri(elasticHostAddress);
            return new ElasticClient(new ConnectionSettings(node));
        }       

        static ClientMetricsEntry PopulateClientMetricsEntry(IClientPerformanceMetrics metricsData, State state)
        {
            return new ClientMetricsEntry
            {
                ConnectedGatewayCount = metricsData.ConnectedGatewayCount,

                ClientId = state.Id,
                DeploymentId = state.DeploymentId,
                Address = state.Address,
                HostName = state.HostName,

                CpuUsage = metricsData.CpuUsage,
                TotalPhysicalMemory = metricsData.TotalPhysicalMemory,
                AvailablePhysicalMemory = metricsData.AvailablePhysicalMemory,
                MemoryUsage = metricsData.MemoryUsage,
                SendQueueLength = metricsData.SendQueueLength,
                ReceiveQueueLength = metricsData.ReceiveQueueLength,
                SentMessages = metricsData.SentMessages,
                ReceivedMessages = metricsData.ReceivedMessages,

                Time = DateTime.UtcNow.ToString(DATE_TIME_FORMAT, CultureInfo.InvariantCulture)                
            };
        }

        static dynamic PopulateStatsTableEntry(ICounter counter, State state)
        {
            var stat = new ExpandoObject() as IDictionary<string, object>;
            stat.Add("ClientId", state.Id);

            stat.Add("DeploymentId", state.DeploymentId);
            stat.Add("HostName", state.HostName);
            stat.Add("UtcDateTime", DateTimeOffset.UtcNow.UtcDateTime);
            stat.Add(counter.Name, counter.IsValueDelta ? float.Parse(counter.GetDeltaString()) : float.Parse(counter.GetValueString()));

            return stat;
        }


    }

}
