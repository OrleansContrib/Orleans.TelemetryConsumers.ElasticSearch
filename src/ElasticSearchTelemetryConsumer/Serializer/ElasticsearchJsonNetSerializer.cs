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

        public ElasticsearchJsonNetSerializer(JsonSerializerSettings settings = null)
        {
            _settings = settings ?? CreateSettings();
            this._defaultSerializer = JsonSerializer.Create(_settings);
        }

        private JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };
            return settings;
        }

        public object Deserialize(Type type, Stream stream)
        {
            var settings = this._settings;
            return _Deserialize(type, stream, settings);
        }

        public T Deserialize<T>(Stream stream)
        {
            var settings = this._settings;
            return (T)_Deserialize(typeof(T), stream, settings);
        }

        public Task<object> DeserializeAsync(Type type, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(_Deserialize(type, stream));
        }

        protected internal object _Deserialize(Type type, Stream stream, JsonSerializerSettings settings = null)
        {
            settings = settings ?? this._settings;
            var serializer = JsonSerializer.Create(settings);
            var jsonTextReader = new JsonTextReader(new StreamReader(stream));
            var t = serializer.Deserialize(jsonTextReader, type);
            return t;
        }

        public Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = this.Deserialize<T>(stream);
            return Task.FromResult(result);
        }

        public void Serialize<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.Indented)
        {
            using (var writer = new StreamWriter(stream, ExpectedEncoding, BufferSize, leaveOpen: true))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                _defaultSerializer.Serialize(jsonWriter, data);
                writer.Flush();
                jsonWriter.Flush();
            }
        }

        public Task SerializeAsync<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.Indented, CancellationToken cancellationToken = default(CancellationToken))
        {
            Serialize(data, stream, formatting);
            return Task.CompletedTask;
        }
    }
}
