# [![Ants](https://github.com/CyAScott/Ants/blob/master/assets/ants-64.png?raw=true "Ants")](https://www.nuget.org/packages/Ants/) Asp.Net Test Server (ANTS)

[![Build status](https://ci.appveyor.com/api/projects/status/sidmvovywlxnkrj2?svg=true)](https://ci.appveyor.com/project/CyAScott/ants) [![NuGet Badge](https://buildstats.info/nuget/Ants)](https://www.nuget.org/packages/Ants/)

ANTS is a framework for running an Asp.Net application in process instead of [IIS](https://www.iis.net/). The target Asp.Net application will accept http requests from a custom [HttpClient](https://msdn.microsoft.com/en-us/library/system.net.http.httpclient(v=vs.118).aspx) created by ANTS. This will allow you to test your application without the need to open up access to sockets.

### Simple Exmaple

The first thing you will need to do is to install the Ants NuGet package. Run the command below in the Package Manager Console.

`Install-Package Ants`

After the package is installed you can run the code below to see a working example:

```
using System.Threading.Tasks;
using Ants;
using Ants.Web;

public static class AntsExample
{
    public static async Task Run()
    {
        //Global is my Asp.Net Http Application class
        AspNetTestServer.Start<Global>(new StartApplicationArgs
        {
            PhysicalDirectory = @"C:\MySolutionFolder\MyWebProjFolder"
        });

        using (var client = AspNetTestServer.GetHttpClient<Global>())
        using (var response = await client.GetAsync("/SomeRoute").ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();
        }
        
        await AspNetTestServer.Stop<Global>().ConfigureAwait(false);
    }
}
```

The example above creates an App Domain for your http application to run in, using the same method that IIS does. Once the application is started, you can get an HttpClient for making http calls to the Asp.Net application.


#### Icon Created By

Icons made by [Freepik](http://www.freepik.com) from [www.flaticon.com](http://www.flaticon.com) is licensed by [CC 3.0 B](http://creativecommons.org/licenses/by/3.0/)
