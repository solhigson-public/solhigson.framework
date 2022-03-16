// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Solhigson.Framework.Benchmarks.Whiteboard;

IServiceProvider BuildDi(IConfiguration config)
{
    return new ServiceCollection()
        //Add DI Classes here
        .AddLogging(loggingBuilder =>
        {
            // configure Logging with NLog
            loggingBuilder.ClearProviders();
            loggingBuilder.AddNLog(config);
        })
        .AddSingleton<Benchmark>()
        .BuildServiceProvider();
}

var config = new ConfigurationBuilder()
    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
    .Build();

var provider = BuildDi(config);
provider.GetRequiredService<Benchmark>().Execute();
BenchmarkRunner.Run(typeof(Program).Assembly);

