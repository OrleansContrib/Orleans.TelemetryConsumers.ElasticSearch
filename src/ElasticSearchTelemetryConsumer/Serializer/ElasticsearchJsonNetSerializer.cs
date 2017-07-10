using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Newtonsoft.Json;


namespace Orleans.TelemetryConsumers.ElasticSearch.Serializer
{
	public class ElasticsearchJsonNetSerializer : IElasticsearchSerializer
	{
		private static readonly Encoding ExpectedEncoding = new UTF8Encoding(false);
		/// <summary>
		/// The size of the buffer to use when writing the serialized request
		/// to the request stream
		/// Performance tests as part of https://github.com/elastic/elasticsearch-net/issues/1899 indicate this 
		/// to be a good compromise buffer size for performance throughput and bytes allocated.
		/// </summary>
		protected virtual int BufferSize => 1024;

		private readonly JsonSerializerSettings _settings;
		private JsonSerializer _defaultSerializer;

		public ElasticsearchJsonNetSerializer(IList<JsonConverter> converters = null)
		{
			//_settings = settings ?? CreateSettings();
			_settings = CreateSettings();

			if (converters != null)
			{
				if (_settings.Converters != null)
				{
					foreach (var converter in converters)
					{
						_settings.Converters.Add(converter);
					}
				}
				else
				{
					_settings.Converters = converters;
				}
			}

			this._defaultSerializer = JsonSerializer.Create(_settings);
		}

		private JsonSerializerSettings CreateSettings()
		{
			var settings = new JsonSerializerSettings()
			{
				DefaultValueHandling = DefaultValueHandling.Include,
				NullValueHandling = NullValueHandling.Ignore,
			};
			return settings;
		}


		public T Deserialize<T>(Stream stream)
		{
			var settings = this._settings;
			return _Deserialize<T>(stream, settings);
		}

		public async Task<T> DeserializeAsync<T>(Stream responseStream, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.Deserialize<T>(responseStream);
		}

		protected internal T _Deserialize<T>(Stream stream, JsonSerializerSettings settings = null)
		{
			settings = settings ?? this._settings;
			var serializer = JsonSerializer.Create(settings);
			var jsonTextReader = new JsonTextReader(new StreamReader(stream));
			var t = (T)serializer.Deserialize(jsonTextReader, typeof(T));
			return t;
		}


		public void Serialize(object data, Stream writableStream, SerializationFormatting formatting = SerializationFormatting.Indented)
		{
			using (var writer = new StreamWriter(writableStream, ExpectedEncoding, BufferSize, leaveOpen: true))
			using (var jsonWriter = new JsonTextWriter(writer))
			{
				_defaultSerializer.Serialize(jsonWriter, data);
				writer.Flush();
				jsonWriter.Flush();
			}
		}

		public IPropertyMapping CreatePropertyMapping(MemberInfo memberInfo) => null;
	}
}
