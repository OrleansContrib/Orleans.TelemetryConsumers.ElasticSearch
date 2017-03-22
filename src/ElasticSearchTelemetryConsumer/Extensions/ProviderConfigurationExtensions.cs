using System;
using System.Collections.Generic;
using Orleans.Runtime.Configuration;

namespace Orleans.Telemetry
{
    public static class ProviderConfigurationExtensions
    {
        public static void AddElasticSearchStatisticsProvider(this ClusterConfiguration config,
            string providerName, Uri ElasticHostAddress, string ElasticIndex= "orleans_statistics", string ElasticMetricType= "metric", string ElasticCounterType = "counter")
        {
            if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentNullException(nameof(providerName));
            if (string.IsNullOrWhiteSpace(ElasticIndex)) throw new ArgumentNullException(nameof(ElasticIndex));
            if (string.IsNullOrWhiteSpace(ElasticMetricType)) throw new ArgumentNullException(nameof(ElasticMetricType));
            if (string.IsNullOrWhiteSpace(ElasticCounterType)) throw new ArgumentNullException(nameof(ElasticCounterType));

            var properties = new Dictionary<string, string>
            {
                {"ElasticHostAddress", ElasticHostAddress.ToString()},
                {"ElasticIndex", ElasticIndex},
                {"ElasticMetricsType", ElasticMetricType},
                {"ElasticCounterType", ElasticCounterType},
            };

            config.Globals.RegisterStatisticsProvider<ElasticStatisticsProvider>(providerName, properties);
        }

        public static void AddElasticSearchStatisticsProvider(this ClientConfiguration config,
            string providerName, Uri ElasticHostAddress, string ElasticIndex = "orleans_statistics", string ElasticType = "metrics")
        {
            if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentNullException(nameof(providerName));
            if (string.IsNullOrWhiteSpace(ElasticIndex)) throw new ArgumentNullException(nameof(ElasticIndex));
            if (string.IsNullOrWhiteSpace(ElasticType)) throw new ArgumentNullException(nameof(ElasticType));

            var properties = new Dictionary<string, string>
            {
                {"ElasticHostAddress", ElasticHostAddress.ToString()},
                {"ElasticIndex", ElasticIndex},
                {"ElasticMetricsType", ElasticType},
            };

            config.RegisterStatisticsProvider<ElasticClientMetricsProvider>(providerName, properties);
        }

    }
}
