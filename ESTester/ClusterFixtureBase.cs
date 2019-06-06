using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElasticsearchInside;
using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Telemetry;
using Orleans.TestingHost;

namespace ESTester
{
    public abstract class ClusterFixtureBase : IDisposable
    {
        private readonly ElasticsearchInside.Elasticsearch _elasticsearch;

        static ClusterFixtureBase()
        {
            //TestClusterOptions.BuildClientConfiguration()
            //TestClusterOptions.DefaultTraceToConsole = false;
        }

        protected ClusterFixtureBase()
        {
            GrainClient.Uninitialize();
            //SerializationManager.InitializeForTesting();

            var testCluster = CreateTestCluster();


            this._elasticsearch = new ElasticsearchInside.Elasticsearch();



            var esTeleM = new ElasticSearchTelemetryConsumer(_elasticsearch.Url, "orleans-telemetry");
            LogManager.TelemetryConsumers.Add(esTeleM);
            LogManager.LogConsumers.Add(esTeleM);

            if (testCluster.Primary == null)
            {
                testCluster.Deploy();
            }
            this.HostedCluster = testCluster;
        }

        protected abstract TestCluster CreateTestCluster();

        public TestCluster HostedCluster { get; private set; }

        public virtual void Dispose()
        {
            GrainClient.Uninitialize();
            //GrainClient.HardKill();
            foreach (var silo in HostedCluster.GetActiveSilos())
                HostedCluster.KillSilo(silo);
            //this.HostedCluster.StopAllSilos();

            _elasticsearch?.Dispose();
        }
    }

}

