using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Custom.Support;

namespace Elastic.Apm.Custom
{
    public class HttpClientTracer : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            if (InnerHandler == null)
                InnerHandler = new HttpClientHandler();

            if (Agent.Tracer.CurrentTransaction == null)
                return await base.SendAsync(request, cancel);

            var transaction = Agent.Tracer.CurrentTransaction;

            var span = transaction.StartSpan(
                $"{request.Method.Method} {request.RequestUri.Host}",
                ApiConstants.TypeExternal,
                ApiConstants.SubtypeHttp);

            if (transaction.IsSampled) {
                span.Context.Http = new Http {
                    Method = request.Method.Method,
                    Url = request.RequestUri.ToString()
                };
            }

            if (!request.Headers.Contains(TraceParent.TraceParentHeaderName))
                request.Headers.Add(TraceParent.TraceParentHeaderName, TraceParent.BuildTraceparent(span.TraceId, span.ParentId, span.IsSampled));

            try
            {
                var httpResponse = await base.SendAsync(request, cancel);

                if (transaction.IsSampled)
                    span.Context.Http.StatusCode = (int)httpResponse.StatusCode;

                span.End();
                return httpResponse;
            }
            catch (Exception ex)
            {
                span.CaptureException(ex);
                throw;
            }
        }
    }
}