using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Elastic.Apm.Api;

namespace Elastic.Apm.Custom.Support
{
    public class DistributionData
    {
        public static DistributedTracingData GetTracingData(string traceId, string parentId, bool flagRecorded)
        {
            //NOTE: Not ideal - this really should just be using the constructor but it is marked private
            var serialized = TraceParent.BuildTraceparent(traceId, parentId, flagRecorded);
            return DistributedTracingData.TryDeserializeFromString(serialized);
        }

        public static DistributedTracingData GetTracingData(HttpRequestMessage request)
        {
            return GetTracingData(ConvertHeaders(request.Headers));
        }

        public static DistributedTracingData GetTracingData(NameValueCollection headers)
        {
            return GetTracingData(ConvertHeaders(headers));
        }

        public static DistributedTracingData GetTracingData(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
                return null;

            var headerValue = headers.ContainsKey(TraceParent.TraceParentHeaderName) ? headers[TraceParent.TraceParentHeaderName] : null;
            if (headerValue != null)
            {
                var data = TraceParent.TryExtractTraceparent(headerValue);

                if (data != null)
                    return data;
            }

            return Agent.Tracer.CurrentTransaction != null 
                ? Agent.Tracer.CurrentTransaction.OutgoingDistributedTracingData 
                : null;
        }

        private static Dictionary<string, string> ConvertHeaders(NameValueCollection headers)
        {
            var convertedHeaders = new Dictionary<string, string>();
            foreach (var key in headers.AllKeys)
            {
                convertedHeaders.Add(key, headers.Get(key));
            }
            return convertedHeaders;
        }

        private static Dictionary<string, string> ConvertHeaders(HttpHeaders httpHeaders)
        {
            var convertedHeaders = new Dictionary<string, string>();
            foreach (var header in httpHeaders)
            {
                convertedHeaders.Add(header.Key, header.Value.FirstOrDefault());
            }
            return convertedHeaders;
        }
    }
}