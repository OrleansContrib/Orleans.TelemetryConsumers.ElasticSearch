using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime.Configuration;
using Orleans.Streams;
using Orleans.TestingHost;
using Xunit;
using Xunit.Abstractions;

namespace ESTester
{

    public class IntegrationTests : IClassFixture<IntegrationTests.Fixture>
    {
        protected const string streamProvider = "stuff";



        private readonly IntegrationTests.Fixture _fixture;
        private readonly ITestOutputHelper _output;

        public IntegrationTests(IntegrationTests.Fixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public async Task GenerateSomeActivity()
        {
            Guid streamId = Guid.NewGuid();

            IStreamProvider streamProviderBrc =
                _fixture.HostedCluster.StreamProviderManager.GetStreamProvider(streamProvider);
            IAsyncStream<object> messageStream =
                streamProviderBrc.GetStream<object>(streamId, streamProvider);


            for (int i = 0; i < 1000; i++)
            {
                await messageStream.OnNextAsync(new
                {
                    junk = "junk",
                    morejunk = 2,
                });

            }
        }



        [Fact]
        public async Task DelayUntilStatistics()
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
        }



        public class Fixture : ClusterFixtureBase
        {
            protected override TestCluster CreateTestCluster()
            {
                TimeSpan _timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

                var options = new TestClusterOptions(1); //default = 2 nodes in cluster

                options.ClusterConfiguration.AddMemoryStorageProvider("PubSubStore");

                options.ClusterConfiguration.AddSimpleMessageStreamProvider(providerName: streamProvider,
                    fireAndForgetDelivery: false);

                options.ClusterConfiguration.ApplyToAllNodes(c => c.DefaultTraceLevel = Orleans.Runtime.Severity.Error);
                options.ClusterConfiguration.ApplyToAllNodes(c => c.TraceToConsole = false);
                options.ClusterConfiguration.ApplyToAllNodes(c => c.TraceFileName = string.Empty);
                options.ClusterConfiguration.ApplyToAllNodes(c => c.TraceFilePattern = string.Empty);
                options.ClusterConfiguration.ApplyToAllNodes(c => c.StatisticsWriteLogStatisticsToTable = false);
                options.ClusterConfiguration.Globals.ClientDropTimeout = _timeout;
                options.ClusterConfiguration.UseStartupType<TestStartup>();

                options.ClientConfiguration.AddSimpleMessageStreamProvider(providerName: streamProvider,
                    fireAndForgetDelivery: false);

                options.ClientConfiguration.DefaultTraceLevel = Orleans.Runtime.Severity.Error;
                options.ClientConfiguration.TraceToConsole = false;
                options.ClientConfiguration.TraceFileName = string.Empty;
                options.ClientConfiguration.ClientDropTimeout = _timeout;


                return new TestCluster(options);
            }



            public class TestStartup
            {

                public IServiceProvider ConfigureServices(IServiceCollection services)
                {

                    return services.BuildServiceProvider();
                }
            }
        }

    }


}

