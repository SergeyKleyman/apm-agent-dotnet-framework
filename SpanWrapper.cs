using System;
using System.Net;
using Elastic.Apm.Api;
using Elastic.Apm.Custom.Support;
using System.Diagnostics;

namespace Elastic.Apm.Custom
{
    public class SpanWrapper: IDisposable
    {
        private ISpan span;
        private ITransaction transaction;
        private Stopwatch timer;

        public static SpanWrapper Create()
        {
            return new SpanWrapper();
        }

        public void SetTracing(WebRequest request)
        {
            if (Agent.Tracer.CurrentTransaction == null)
                return;

            if (!string.IsNullOrWhiteSpace(request.Headers[TraceParent.TraceParentHeaderName]))
                return;

            timer = new Stopwatch();
            timer.Start();

            transaction = Agent.Tracer.CurrentTransaction;

            span = transaction.StartSpan(
                $"{request.Method} {request.RequestUri.Host}",
                "proxy",
                ApiConstants.SubtypeHttp);

            if (transaction.IsSampled)
            {
                span.Context.Http = new Http
                {
                    Method = request.Method,
                    Url = request.RequestUri.ToString()
                };
            }

            request.Headers.Add(TraceParent.TraceParentHeaderName, TraceParent.BuildTraceparent(span.TraceId, span.ParentId, span.IsSampled));
        }

        public void SetStatus(HttpStatusCode status)
        {
            if (span == null)
                return;

            if (transaction?.IsSampled == true)
                span.Context.Http.StatusCode = (int) status;
        }

        public void Error(Exception ex)
        {
            span?.CaptureException(ex);
        }

        public void Dispose()
        {
            if (span == null)
                return;

            if (timer?.IsRunning == true) {
                timer.Stop();
                span.Duration = timer.ElapsedMilliseconds;
            }

            span.End();
        }
    }
}