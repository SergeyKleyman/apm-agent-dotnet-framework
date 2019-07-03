using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Elastic.Apm.Api;
using Elastic.Apm.Custom.Support;

namespace Elastic.Apm.Custom
{
    public class ApmTracingFilter: IActionFilter
    {
        private const string ApmCacheKey = "APM-Transaction";
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var transaction = GetTransaction(filterContext);

            if (transaction.IsSampled)
                RecordRequest(filterContext, transaction);
            
            filterContext.HttpContext.Items[ApmCacheKey] = transaction;
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var transaction = filterContext.HttpContext.Items[ApmCacheKey] as ITransaction;

            if (transaction?.IsSampled == true)
                RecordResponse(filterContext, transaction);

            transaction?.End();
        }

        public static ITransaction GetTransaction(ActionExecutingContext context)
        {
            var request = context.RequestContext.HttpContext.Request;
            var data = DistributionData.GetTracingData(request.Headers);
            var name = $"{request.HttpMethod} {request.RawUrl}";
            return Agent.Tracer.StartTransaction(name, ApiConstants.TypeRequest, data);
        }

        public static void RecordRequest(ActionExecutingContext context, ITransaction transaction)
        {
            var request = context.RequestContext.HttpContext.Request;
            var url = Support.UrlHelper.GetUrl(request.Url);

            transaction.Context.Request = new Request(request.HttpMethod, url)
            {
                Socket = new Socket
                {
                    Encrypted = context.HttpContext.Request.IsSecureConnection,
                    RemoteAddress = context.HttpContext.Request.UserHostAddress
                },
                Headers = Agent.Config.CaptureHeaders ? ConvertHeaders(request.Headers) : null
            };
        }

        private static string GetUserName(HttpContextBase context)
        {
            if (context?.User?.Identity?.IsAuthenticated == true)
                return context.User.Identity.Name;

            var user = Thread.CurrentPrincipal;
            if (user?.Identity?.IsAuthenticated == true)
                return user.Identity.Name;

            return string.Empty;
        }

        public static void RecordResponse(ActionExecutedContext context, ITransaction transaction)
        {
            var response = context.HttpContext.Response;

            transaction.Context.User = new User
            {
                UserName = GetUserName(context.HttpContext)
            };

            transaction.Context.Response = new Response
            {
                Finished = true,
                StatusCode = response.StatusCode,
                Headers = Agent.Config.CaptureHeaders ? ConvertHeaders(response.Headers) : null
            };
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
    }
}