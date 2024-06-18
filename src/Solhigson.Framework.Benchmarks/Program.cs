// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Solhigson.Framework.Benchmarks;

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
        .BuildServiceProvider();
}

var config = new ConfigurationBuilder()
    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
    .Build();

BuildDi(config);


//BenchmarkRunner.Run<StringTest>();


