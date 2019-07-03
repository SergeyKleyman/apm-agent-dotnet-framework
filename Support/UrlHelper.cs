using System;
using Elastic.Apm.Api;

namespace Elastic.Apm.Custom.Support
{
    public class UrlHelper
    {
        public static Url GetUrl(Uri httpRequestUrl)
        {
            return new Url
            {
                Full = httpRequestUrl.AbsoluteUri,
                HostName = httpRequestUrl.Host,
                Protocol = httpRequestUrl.Scheme,
                Raw = httpRequestUrl.OriginalString
            };
        }
    }
}