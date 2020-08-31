using Elasticsearch.Net;
using Orleans.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Orleans.Serialization;
using Orleans.TelemetryConsumers.ElasticSearch.Serializer;
using Microsoft.Extensions.Options;
using Orleans.TelemetryConsumers.ElasticSearch;

namespace Orleans.Telemetry
{ 
	class TelemetryRecord
	{
		public JObject tm { get; set; }
		public string IndexName { get; set; }
		public string IndexType { get; set; }
	}



	public class ElasticsearchTelemetryConsumer :
		IMetricTelemetryConsumer,
		//ITraceTelemetryConsumer, // we dont need to handle trace, its seen via System.Diagnostics.Trace, which likely will be obsoleted by orleans
		IEventTelemetryConsumer,
		IExceptionTelemetryConsumer,
		IDependencyTelemetryConsumer,
		IRequestTelemetryConsumer
	{
        private const string DocumentType = "doc";
		private readonly Uri _elasticSearchUri;
		private readonly string _indexPrefix;

		private readonly BlockingCollection<TelemetryRecord> _queueToBePosted = new BlockingCollection<TelemetryRecord>();
		private IElasticLowLevelClient _client;
		private readonly string _dateFormatter;
		private readonly object _machineName;

        public ElasticsearchTelemetryConsumer(IOptions<ElasticsearchTelemetryConsumerOptions> options)
		{
            _elasticSearchUri = options.Value.ElasticSearchUri;
			_indexPrefix = options.Value.IndexPrefix;
			_dateFormatter = options.Value.DateFormatter;

			_machineName = Environment.MachineName;

			SetupObserverBatchy(TimeSpan.FromSeconds(options.Value.BufferWaitSeconds), options.Value.BufferSize);
		}

		public IElasticLowLevelClient GetClient(Uri esurl)
		{
			if (_client != null)
			{
				return _client;
			}
			else
			{
				var singleNode = new SingleNodeConnectionPool(esurl);

				var cc = new ConnectionConfiguration(singleNode, new ElasticsearchJsonNetSerializer())
					.EnableHttpPipelining()
					.ThrowExceptions();

				//the 1.x serializer we needed to use, as the default SimpleJson didnt work right
				//Elasticsearch.Net.JsonNet.ElasticsearchJsonNetSerializer()

				this._client = new ElasticLowLevelClient(cc);
				return this._client;
			}
		}

        private void SetupObserverBatchy(TimeSpan waitTime, int bufferSize)
        {
            this._queueToBePosted.GetConsumingEnumerable()
                .ToObservable(Scheduler.Default)
                .Buffer(waitTime, bufferSize)
                .Subscribe(async (x) => await this.ElasticSearchBulkWriter(x));
        }


        //private string ElasticIndex() => _indexPrefix + "-" + DateTime.UtcNow.ToString(_dateFormatter);
        //private string ElastiTraceTelemetryType() => "trace"; //obsoleted

        //private string ElasticMetricTelemetryType() => "metric";
        private string ElasticMetricTelemetryIndex() => _indexPrefix + "-metric-" + DateTime.UtcNow.ToString(_dateFormatter);
	    //private string ElasticEventTelemetryType() => "event";
	    private string ElasticEventTelemetryIndex() => _indexPrefix + "-event-" + DateTime.UtcNow.ToString(_dateFormatter);
        //private string ElasticExceptionTelemetryType() => "exception";
	    private string ElasticExceptionTelemetryIndex() => _indexPrefix + "-exception-" + DateTime.UtcNow.ToString(_dateFormatter);
        //private string ElasticDependencyTelemetryType() => "dependency";
	    private string ElasticDependencyTelemetryIndex() => _indexPrefix + "-dependency-" + DateTime.UtcNow.ToString(_dateFormatter);
        //private string ElasticRequestTelemetryType() => "request";
	    private string ElasticRequestTelemetryIndex() => _indexPrefix + "-request-" + DateTime.UtcNow.ToString(_dateFormatter);
        //private string ElasticLogType() => "log";
        private string ElasticLogIndex() => _indexPrefix + "-log-" + DateTime.UtcNow.ToString(_dateFormatter);

  //      #region IFlushableLogConsumer

  //      public void Log(Severity severity, LoggerType loggerType, string caller, string message, IPEndPoint myIPEndPoint,
		//	Exception exception, int eventCode = 0)
		//{
		//	Task.Run(async () =>
		//	{
		//		var tm = new ExpandoObject() as IDictionary<string, Object>;
		//		tm.Add("Severity", severity.ToString());
		//		tm.Add("LoggerType", loggerType.ToString());
		//		tm.Add("Caller", caller);
		//		tm.Add("Message", message);
		//		tm.Add("IPEndPoint", myIPEndPoint?.ToString());
		//		tm.Add("Exception", exception?.ToString());
		//		tm.Add("EventCode", eventCode);

		//		await FinalESWrite(tm, ElasticLogIndex);
		//	});

		//}

		//#endregion

		#region IExceptionTelemetryConsumer
		public void TrackException(Exception exception, IDictionary<string, string> properties = null,
			IDictionary<string, double> metrics = null)
		{
			Task.Run(async () =>
			{
				var tm = new ExpandoObject() as IDictionary<string, Object>;
				tm.Add("Exception", exception.ToString());
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

				await FinalESWrite(tm, ElasticExceptionTelemetryIndex);
			});
		}

		#endregion

		//#region ITraceTelemetryConsumer

		//public void TrackTrace(string message)
		//{
		//    TrackTrace(message, Severity.Info);
		//}

		//public void TrackTrace(string message, IDictionary<string, string> properties)
		//{
		//    if (properties != null)
		//    {
		//        TrackTrace(message, Severity.Info, properties);
		//    }
		//    else
		//    {
		//        TrackTrace(message);
		//    }
		//}

		//public void TrackTrace(string message, Severity severity)
		//{
		//    TrackTrace(message, severity, null);
		//}

		//public void TrackTrace(string message, Severity severity, IDictionary<string, string> properties)
		//{
		//    WriteTrace(message, severity, properties);
		//}

		//public async Task WriteTrace(string message, Severity severity, IDictionary<string, string> properties)
		//{
		//    var tm = new ExpandoObject() as IDictionary<string, Object>;
		//    tm.Add("Message", message);
		//    tm.Add("Severity", severity.ToString());
		//    if (properties != null)
		//    {
		//        foreach (var prop in properties)
		//        {
		//            tm.Add(prop.Key, prop.Value);
		//        }
		//    }

		//    await FinalESWrite(tm, ElasticTraceTelemetryType);
		//}


		//#endregion

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
				name = name.Replace("..", ".");
				tm.Add(name, value);
				if (properties != null)
				{
					foreach (var prop in properties)
					{
						tm.Add(prop.Key, prop.Value);
					}
				}
				await FinalESWrite(tm, ElasticMetricTelemetryIndex);
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

				await FinalESWrite(tm, ElasticDependencyTelemetryIndex);
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

				await FinalESWrite(tm, ElasticRequestTelemetryIndex);
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

				await FinalESWrite(tm, ElasticEventTelemetryIndex);
			});
		}

		#endregion


		#region FinalWrite

		private async Task FinalESWrite(IDictionary<string, Object> tm, Func<string> index)
		{
			tm.Add("UtcDateTime", DateTimeOffset.UtcNow.UtcDateTime);
			tm.Add("MachineName", _machineName);

			try
			{
				//convert tm to JObject
				var jo = JObject.FromObject(tm);

				_queueToBePosted.Add(new TelemetryRecord()
				{
					tm = jo,
					IndexName = index(),
					IndexType = DocumentType
				});
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}
		}



		private async Task ElasticSearchBulkWriter(IEnumerable<TelemetryRecord> jos)
		{
			if (jos.Count() < 1)
				return;

			var actionMeta = jos.Select(o => new { index = new { _index = o.IndexName, _type = o.IndexType } });
			var actionMetaSource = jos.Zip(actionMeta, (f, s) => new object[] { s, f.tm });
			var bbo = actionMetaSource.SelectMany(a => a);

			try
			{
				var ret = await GetClient(this._elasticSearchUri)
					.BulkPutAsync<VoidResponse>(
						PostData.MultiJson(bbo.ToArray()),
						new BulkRequestParameters { Refresh = Refresh.False });
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		#endregion


		public void Flush()
		{ }

		public void Close()
		{ }
	}
}