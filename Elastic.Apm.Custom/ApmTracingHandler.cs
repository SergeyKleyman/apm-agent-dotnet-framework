using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Custom.Support;

namespace Elastic.Apm.Custom
{
    public class ApmTracingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            if (InnerHandler == null)
                InnerHandler = new HttpClientHandler();

            var transaction = GetTransaction(request);

            if (!request.Headers.Contains(TraceParent.TraceParentHeaderName))
            {
                //Outgoing request
                var traceString = TraceParent.BuildTraceparent(transaction.TraceId, transaction.ParentId, transaction.IsSampled);
                request.Headers.Add(TraceParent.TraceParentHeaderName, traceString);
            }

            if (transaction.IsSampled)
                RecordRequest(request, transaction);

            var httpResponse = await base.SendAsync(request, cancel);

            transaction.Result = $"HTTP {(int)httpResponse.StatusCode}";

            if (transaction.IsSampled)
                RecordResponse(httpResponse, transaction);

            transaction.End();
            return httpResponse;
        }

        public static ITransaction GetTransaction(HttpRequestMessage request)
        {
            var data = DistributionData.GetTracingData(request);
            var name = $"{request.Method.Method} {request.RequestUri.OriginalString}";
            return Agent.Tracer.StartTransaction(name, ApiConstants.TypeRequest, data);
        }

        public static void RecordRequest(HttpRequestMessage httpRequest, ITransaction transaction)
        {
            var url = UrlHelper.GetUrl(httpRequest.RequestUri);

            transaction.Context.Request = new Request(httpRequest.Method.Method, url)
            {
                Socket = new Socket
                {
                    Encrypted = httpRequest.RequestUri.Scheme == "HTTPS",
                    RemoteAddress = GetClientIpAddress(httpRequest)
                },

                Headers = Agent.Config.CaptureHeaders ? ConvertHeaders(httpRequest.Headers) : null
            };
        }

        private static string GetClientIpAddress(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                dynamic context = request.Properties["MS_HttpContext"];

                if (context?.Request?.UserHostAddress != null)
                    return IPAddress.Parse(context.Request.UserHostAddress).ToString();
            }

            if (request.Properties.ContainsKey("MS_OwinContext"))
            {
                dynamic context = request.Properties["MS_OwinContext"];
                if (context?.Request?.RemoteIpAddress != null)
                    return IPAddress.Parse(context.Request.RemoteIpAddress).ToString();
            }

            return string.Empty;
        }

        public static void RecordResponse(HttpResponseMessage httpResponse, ITransaction transaction) =>
            transaction.Context.Response = new Response {
                Finished = true,
                StatusCode = (int)httpResponse.StatusCode,
                Headers = Agent.Config.CaptureHeaders ? ConvertHeaders(httpResponse.Headers) : null
            };

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