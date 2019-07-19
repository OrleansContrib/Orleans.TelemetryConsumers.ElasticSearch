using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Telemetry;
using System;

namespace Orleans.TelemetryConsumers.ElasticSearch
{
    public static class ElasticsearchTelemetryConsumerConfigurationExtensions
    {
        /// <summary>
        /// Adds a metrics telemetric consumer provider of type <see cref="ElasticsearchTelemetryConsumer"/>.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="instrumentationKey">The Application Insights instrumentation key.</param>
        public static ISiloHostBuilder AddElasticsearchTelemetryConsumer(this ISiloHostBuilder hostBuilder, Uri elasticSearchUri, string indexPrefix = "orleans-telemetry", string dateFormatter = "yyyy-MM-dd-HH", int bufferWaitSeconds = 1, int bufferSize = 50)
        {
            return hostBuilder.ConfigureServices((context, services) => ConfigureServices(context, services, elasticSearchUri, indexPrefix, dateFormatter, bufferWaitSeconds, bufferSize));
        }

        /// <summary>
        /// Adds a metrics telemetric consumer provider of type <see cref="ElasticsearchTelemetryConsumer"/>.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="instrumentationKey">The Application Insights instrumentation key.</param>
        public static ISiloBuilder AddElasticsearchTelemetryConsumer(this ISiloBuilder hostBuilder, Uri elasticSearchUri, string indexPrefix = "orleans-telemetry", string dateFormatter = "yyyy-MM-dd-HH", int bufferWaitSeconds = 1, int bufferSize = 50)
        {
            return hostBuilder.ConfigureServices((context, services) => ConfigureServices(context, services, elasticSearchUri, indexPrefix, dateFormatter, bufferWaitSeconds, bufferSize));
        }

        /// <summary>
        /// Adds a metrics telemetric consumer provider of type <see cref="ElasticsearchTelemetryConsumer"/>.
        /// </summary>
        /// <param name="clientBuilder"></param>
        /// <param name="instrumentationKey">The Application Insights instrumentation key.</param>
        public static IClientBuilder AddElasticsearchTelemetryConsumer(this IClientBuilder clientBuilder, Uri elasticSearchUri, string indexPrefix = "orleans-telemetry", string dateFormatter = "yyyy-MM-dd-HH", int bufferWaitSeconds = 1, int bufferSize = 50)
        {
            return clientBuilder.ConfigureServices((context, services) => ConfigureServices(context, services, elasticSearchUri, indexPrefix, dateFormatter, bufferWaitSeconds, bufferSize));
        }

        private static void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context, IServiceCollection services, Uri elasticSearchUri, string indexPrefix, string dateFormatter, int bufferWaitSeconds, int bufferSize)
        {
            services.ConfigureFormatter<ElasticsearchTelemetryConsumerOptions>();
            services.Configure<TelemetryOptions>(options => options.AddConsumer<ElasticsearchTelemetryConsumer>());
            services.Configure<ElasticsearchTelemetryConsumerOptions>(options =>
            {
                options.ElasticSearchUri = elasticSearchUri;
                options.IndexPrefix = indexPrefix;
                options.DateFormatter = dateFormatter;
                options.BufferWaitSeconds = bufferWaitSeconds;
                options.BufferSize = bufferSize;
            });
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services, Uri elasticSearchUri, string indexPrefix, string dateFormatter, int bufferWaitSeconds, int bufferSize)
        {
            services.ConfigureFormatter<ElasticsearchTelemetryConsumerOptions>();
            services.Configure<TelemetryOptions>(options => options.AddConsumer<ElasticsearchTelemetryConsumer>());
            services.Configure<ElasticsearchTelemetryConsumerOptions>(options =>
            {
                options.ElasticSearchUri = elasticSearchUri;
                options.IndexPrefix = indexPrefix;
                options.DateFormatter = dateFormatter;
                options.BufferWaitSeconds = bufferWaitSeconds;
                options.BufferSize = bufferSize;
            });
        }
    }
}
