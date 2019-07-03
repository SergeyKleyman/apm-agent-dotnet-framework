# apm-agent-dotnet-framework
Custom Handlers and Filters to enable Elastic APM tracing in .NET Framework

Example .NET Framework Web API integration via the MessageHandler pipeline

```c#
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        // Configure Web API for self-host. 
        HttpConfiguration config = new HttpConfiguration();
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{action}/{id}",
            defaults: new { id = RouteParameter.Optional }
        );

        config.MessageHandlers.Add(new ApmTracingHandler());
        app.UseWebApi(config);
    }
}
```