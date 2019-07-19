using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.TelemetryConsumers.ElasticSearch
{
    public class ElasticsearchTelemetryConsumerOptions
    {
        public Uri ElasticSearchUri;
        public string IndexPrefix;
        public string DateFormatter;
        public int BufferWaitSeconds;
        public int BufferSize;
    }
}
