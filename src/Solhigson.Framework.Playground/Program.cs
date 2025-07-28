using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;
using Solhigson.Framework.Logging.Nlog;
using Solhigson.Framework.Web;

namespace Solhigson.Framework.Playground;

public partial class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.UseNLog()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                //logging.SetMinimumLevel(LogLevel.Trace);

                // logging.AddNLog(new NLogProviderOptions {
                //     // <-- merge scopes into LogEventInfo.Properties
                //     IncludeScopes            = true,
                //     // <-- capture structured-message template parameters too
                //     CaptureMessageTemplates  = true,
                //     CaptureMessageProperties = true,
                //     
                //     
                //     
                // });
                
                var nlogProvider = new NLogLoggerProvider(new NLogProviderOptions {
                    IncludeScopes            = true,
                    CaptureMessageTemplates  = true,
                    CaptureMessageProperties = true
                });
                
                
                logging.AddProvider(new SolhigsonLoggerProvider(
                    innerProvider: nlogProvider));
                
            });
            
            builder.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

        return builder;
    }
}