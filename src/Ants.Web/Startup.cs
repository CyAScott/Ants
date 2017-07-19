using System;
using System.Linq;
using System.Web;
using Ants.Web;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Owin;

#pragma warning disable 1591

[assembly: OwinStartup(typeof(Startup))]

namespace Ants.Web
{
    public class Startup
    {
        private static readonly object locker = new object();

        public void Configuration(IAppBuilder app)
        {
            foreach (var stage in Enum.GetValues(typeof(PipelineStage))
                .Cast<int>()
                .OrderBy(value => value)
                .Cast<PipelineStage>())
            {
                app.Use((context, next) =>
                {
                    if (!string.Equals(context.Request.Uri.AbsolutePath, "/owin/test", StringComparison.OrdinalIgnoreCase))
                    {
                        return next.Invoke();
                    }

                    lock (locker)
                    {
                        const string startedKey = "startedOwinTest";
                        if (!HttpContext.Current.Items.Contains(startedKey))
                        {
                            HttpContext.Current.Items[startedKey] = null;
                            context.Response.OnSendingHeaders(state =>
                            {
                                context.Response.StatusCode = 200;
                                context.Response.ReasonPhrase = "OK";
                            }, null);
                        }
                    }

                    context.Response.Headers.Append($"Owin-{stage}", ((int)stage).ToString());

                    return next.Invoke();
                });
                app.UseStageMarker(stage);
            }
        }
    }
}