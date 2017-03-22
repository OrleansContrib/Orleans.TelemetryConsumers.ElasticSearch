using Elasticsearch.Net;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net.Connection;
using Nest;
using Orleans.Serialization;

namespace Orleans.Telemetry
{
    public class ElasticSearchTelemetryConsumer : 
        IMetricTelemetryConsumer,
        ITraceTelemetryConsumer,
        IEventTelemetryConsumer,
        IExceptionTelemetryConsumer,
        IDependencyTelemetryConsumer,
        IRequestTelemetryConsumer,
        IFlushableLogConsumer
    {
        private readonly Uri _elasticSearchUri;
        private readonly string _indexPrefix;
        private readonly ElasticClient _client;

        public ElasticSearchTelemetryConsumer(Uri elasticSearchUri, string indexPrefix)
        {
            _elasticSearchUri = elasticSearchUri;
            _indexPrefix = indexPrefix;

            var orl = OrleansJsonSerializer.GetDefaultSerializerSettings();

            var cs = new ConnectionSettings(_elasticSearchUri)
                .EnableHttpCompression()
                .SetJsonSerializerSettingsModifier(s => s.Converters = orl.Converters);

            //cs.AddContractJsonConverters(t=>)

            var y = new Elasticsearch.Net.JsonNet.ElasticsearchJsonNetSerializer();

            //foreach (var jsonConverter in x.Converters)
            //{
              //  cs. .AddContractJsonConverter(jsonConverter);
            //}

            var cc = new ConnectionConfiguration(_elasticSearchUri)
                .ThrowOnElasticsearchServerExceptions();
            var x = new ElasticsearchClient(cc,
                null, null, new Elasticsearch.Net.JsonNet.ElasticsearchJsonNetSerializer());

            this._client = new ElasticClient(cs);

        }


        private string ElasticIndex() => _indexPrefix + "-" + DateTime.UtcNow.ToString("yyyy-MM-dd-HH");
        private string ElasticMetricTelemetryType() => "metric";
        private string ElastiTraceTelemetryType() => "trace";
        private string ElasticEventTelemetryType() => "event";
        private string ElasticExceptionTelemetryType() => "exception";
        private string ElasticDependencyTelemetryType() => "dependency";
        private string ElasticRequestTelemetryType() => "request";
        private string ElasticLogType() => "log";

        #region IFlushableLogConsumer

        public void Log(Severity severity, LoggerType loggerType, string caller, string message, IPEndPoint myIPEndPoint,
            Exception exception, int eventCode = 0)
        {
            Task.Run(async () =>
            {
                var tm = new ExpandoObject() as IDictionary<string, Object>;
                tm.Add("Severity", severity.ToString());
                tm.Add("LoggerType", loggerType);
                tm.Add("Caller", caller);
                tm.Add("Message", message);
                tm.Add("IPEndPoint", myIPEndPoint);
                tm.Add("Exception", exception);
                tm.Add("EventCode", eventCode);

                await FinalESWrite(tm, ElasticLogType);
            });

        }

        #endregion

        #region IExceptionTelemetryConsumer
        public void TrackException(Exception exception, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null)
        {
            Task.Run(async () =>
            {
                var tm = new ExpandoObject() as IDictionary<string, Object>;
                tm.Add("Exception", exception);
                tm.Add("Message", exception.Message);
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        tm.Add(prop.Key, prop.Value);
                    }
                }
                if (metrics != null)
                {
                    foreach (var prop in metrics)
                    {
                        tm.Add(prop.Key, prop.Value);
                    }
                }

                await FinalESWrite(tm, ElasticExceptionTelemetryType);
            });
        }

        #endregion

        #region ITraceTelemetryConsumer

        public void TrackTrace(string message)
        {
            TrackTrace(message, Severity.Info);
        }

        public void TrackTrace(string message, IDictionary<string, string> properties)
        {
            if (properties != null)
            {
                TrackTrace(message, Severity.Info, properties);
            }
            else
            {
                TrackTrace(message);
            }
        }

        public void TrackTrace(string message, Severity severity)
        {
            TrackTrace(message, severity, null);
        }

        public void TrackTrace(string message, Severity severity, IDictionary<string, string> properties)
        {
            WriteTrace(message, severity, properties);
        }

        public async Task WriteTrace(string message, Severity severity, IDictionary<string, string> properties)
        {
            var tm = new ExpandoObject() as IDictionary<string, Object>;
            tm.Add("Message", message);
            tm.Add("Severity", severity.ToString());
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    tm.Add(prop.Key, prop.Value);
                }
            }

            await FinalESWrite(tm, ElastiTraceTelemetryType);
        }


        #endregion


        #region IMetricTelemetryConsumer

        public void DecrementMetric(string name)
        {
            WriteMetric(name, -1, null);
        }

        public void DecrementMetric(string name, double value)
        {
            WriteMetric(name, value * -1, null);
        }

        public void IncrementMetric(string name)
        {
            WriteMetric(name, 1, null);
        }

        public void IncrementMetric(string name, double value)
        {
            WriteMetric(name, value, null);
        }

        public void TrackMetric(string name, TimeSpan value, IDictionary<string, string> properties = null)
        {
            WriteMetric(name, value.TotalMilliseconds, properties);
        }

        public void TrackMetric(string name, double value, IDictionary<string, string> properties = null)
        {
            WriteMetric(name, value, properties);
        }


        public void WriteMetric(string name, double value, IDictionary<string, string> properties = null)
        {
            Task.Run(async () =>
            {
                var tm = new ExpandoObject() as IDictionary<string, Object>;
                //tm.Add("Name", name);
                //tm.Add("Value", value);
                tm.Add(name, value);
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        tm.Add(prop.Key, prop.Value);
                    }
                }

                await FinalESWrite(tm, ElasticMetricTelemetryType);
            });
        }


        #endregion

        #region IDependencyTelemetryConsumer

        public void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime,
            TimeSpan duration, bool success)
        {
            Task.Run(async () =>
            {
                var tm = new ExpandoObject() as IDictionary<string, Object>;
                tm.Add("DependencyName", dependencyName);
                tm.Add("CommandName", commandName);
                tm.Add("StartTime", startTime);
                tm.Add("Duration", duration);
                tm.Add("Success", success);

                await FinalESWrite(tm, ElasticDependencyTelemetryType);
            });
        }

        #endregion

        #region IRequestTelemetryConsumer

        public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode,
            bool success)
        {
            Task.Run(async () =>
            {
                var tm = new ExpandoObject() as IDictionary<string, Object>;
                tm.Add("Request", name);
                tm.Add("StartTime", startTime);
                tm.Add("Duration", duration);
                tm.Add("ResponseCode", responseCode);
                tm.Add("Success", success);

                await FinalESWrite(tm, ElasticRequestTelemetryType);
            });
        }

        #endregion


        #region IEventTelemetryConsumer
        public void TrackEvent(string eventName, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null)
        {
            Task.Run(async () =>
            {
                var tm = new ExpandoObject() as IDictionary<string, Object>;
                tm.Add("Eventname", eventName);
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        tm.Add(prop.Key, prop.Value);
                    }
                }
                if (metrics != null)
                {
                    foreach (var prop in metrics)
                    {
                        tm.Add(prop.Key, prop.Value);
                    }
                }

                await FinalESWrite(tm, ElasticEventTelemetryType);
            });
        }

        #endregion


        #region FinalWrite

        private async Task FinalESWrite(IDictionary<string, object> tm, Func<string> type)
        {
            tm.Add("UtcDateTime", DateTimeOffset.UtcNow.UtcDateTime);
            tm.Add("MachineName", Environment.MachineName);

            var response = await _client.IndexAsync(tm, (ds) => ds.Index(ElasticIndex())
                .Type(type()));
        }



        #endregion


        public void Flush()
        {
        }

        public void Close()
        {
        }
    }


}