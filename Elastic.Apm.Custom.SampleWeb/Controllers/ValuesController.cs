using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Elastic.Apm.Custom.SampleWeb.Controllers
{
    public class ValuesController : ApiController
    {
        public string[] GetValues()
        {
            return new [] {
                "Value 1",
                "Value 2"
            };
        }
    }
}
