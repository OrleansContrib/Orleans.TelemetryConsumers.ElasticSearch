using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.TestingHost;
using Orleans.Telemetry;
using Xunit.Abstractions;
using Orleans.TelemetryConsumers.ElasticSearch;

namespace ESTester
{
    public class ClusterFixture : IDisposable
    {
        public static ElasticsearchInside.Elasticsearch _elasticsearch;
        public static Uri ESEndpoint => _elasticsearch.Url;
        private readonly IMessageSink _messageSink;


        /// <summary>
        /// Identifier for this test cluster instance to facilitate parallel testing with multiple clusters that need fake services.
        /// </summary>
        public string TestClusterId { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Exposes the shared cluster for unit tests to use.
        /// </summary>
        public TestCluster Cluster { get; }

        public ClusterFixture(IMessageSink messageSink)
        {
            _messageSink = messageSink;

            _elasticsearch = new ElasticsearchInside.Elasticsearch(c => c.SetElasticsearchStartTimeout(6000)
                .EnableLogging()
                .LogTo(s => _messageSink.OnMessage(new Xunit.Sdk.DiagnosticMessage(s ?? string.Empty))));

            _elasticsearch.ReadySync();



            var builder = new TestClusterBuilder();

            // add the cluster id for this instance
            // this allows the silos to safely lookup shared data for this cluster deployment
            // without this we can only share data via static properties and that messes up parallel testing
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { nameof(TestClusterId), TestClusterId },
                    { nameof(ESEndpoint), _elasticsearch.Url.ToString() }
                });
            });

            // a configurator allows the silos to configure themselves
            // at this time, configurators cannot take injected parameters
            // therefore we must other means of sharing objects as you can see above
            builder.AddSiloBuilderConfigurator<SiloBuilderConfigurator>();
            builder.AddClientBuilderConfigurator<ClientBuilderConfigurator>();

            Cluster = builder.Build();
            Cluster.Deploy();


            //var esTeleM = new ElasticSearchTelemetryConsumer(_elasticsearch.Url, "orleans-telemetry");
            //LogManager.TelemetryConsumers.Add(esTeleM);
            //LogManager.LogConsumers.Add(esTeleM);

        }

        private class ClientBuilderConfigurator : IClientBuilderConfigurator
        {
            public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
            {
                var testclusterOptions = configuration.GetTestClusterOptions();


                clientBuilder.AddSimpleMessageStreamProvider(name: "stuff", options => options.FireAndForgetDelivery = false);
                clientBuilder.AddElasticsearchTelemetryConsumer(ESEndpoint);
            }
        }

        private class SiloBuilderConfigurator : ISiloBuilderConfigurator
        {
            private string _esEndpoint;

            public void Configure(ISiloHostBuilder hostBuilder)
            {
                hostBuilder.ConfigureServices(services =>
                {
                    //// add the fake storage provider as default in a way that lets us extract it afterwards
                    //services.AddSingleton(_ => new FakeGrainStorage());
                    //services.AddSingleton<IGrainStorage>(_ => _.GetService<FakeGrainStorage>());

                    //// add the fake timer registry in a way that lets us extract it afterwards
                    //services.AddSingleton<FakeTimerRegistry>();
                    //services.AddSingleton<ITimerRegistry>(_ => _.GetService<FakeTimerRegistry>());

                    //// add the fake reminder registry in a way that lets us extract it afterwards
                    //services.AddSingleton<FakeReminderRegistry>();
                    //services.AddSingleton<IReminderRegistry>(_ => _.GetService<FakeReminderRegistry>());
                });

                hostBuilder.AddElasticsearchTelemetryConsumer(ESEndpoint);
                hostBuilder.AddSimpleMessageStreamProvider(name: "stuff", options => options.FireAndForgetDelivery = false);


                hostBuilder.AddMemoryGrainStorageAsDefault();
                hostBuilder.AddMemoryGrainStorage(name: "PubSubStore");

                hostBuilder.UseServiceProviderFactory(services =>
                {
                    var provider = services.BuildServiceProvider();
                    var config = provider.GetService<IConfiguration>();

                    // grab the cluster id that owns this silo
                    var clusterId = config[nameof(TestClusterId)];

                    _esEndpoint = config[nameof(ESEndpoint)];

                //hostBuilder.AddElasticsearchTelemetryConsumer(new Uri(_esEndpoint));
                    //// extract the fake services from the silo so unit tests can access them
                    //GrainStorageGroups[clusterId].Add(provider.GetService<FakeGrainStorage>());
                    //TimerRegistryGroups[clusterId].Add(provider.GetService<FakeTimerRegistry>());
                    //ReminderRegistryGroups[clusterId].Add(provider.GetService<FakeReminderRegistry>());

                    return provider;
                });


            }
        }


        public void Dispose()
        {
            Cluster.StopAllSilos();
            _elasticsearch?.Dispose();
        }
    }


}

