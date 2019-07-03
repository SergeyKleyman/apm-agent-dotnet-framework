# apm-agent-dotnet-framework
Custom Handlers and Filters to enable Elastic APM tracing in .NET Framework

The official APM dotnet client so far is very .CORE orientated.
In order to integrate into a more traditional .NET Framework code base I created the following APM Adaptors.

## ASP.NET Web API integration via the MessageHandler pipeline

```c#
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        // Configure Web API for APM Tracing

        config.MessageHandlers.Add(new ApmTracingHandler());
        app.UseWebApi(config);
    }
}
```

## ASP.NET MVC integration via the GlobalFilters pipeline

```c#
public class Global : HttpApplication
{
    void Application_Start(object sender, EventArgs e)
    {
        // Code that runs on application startup

        GlobalFilters.Filters.Add(new ApmTracingFilter());
    }
}
```

## HttpClient integration via HttpMessageHandler

```c#
public class HttpClientFactory
{
    HttpClient GetClient(bool traceRequest)
    {
	    if (traceRequest)
        {
            var tracingHandler = new HttpClientTracer();
            return new HttpClient(tracingHandler);;
        }

        return new HttpClient();
    }
}
```