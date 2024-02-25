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

// void Stuff()
// {
//     var SessionId = "000014230321095911236616516562";
//     Console.WriteLine(SessionId[..6]);
//     Console.WriteLine(StringPool.Shared.GetOrAdd(SessionId.AsSpan(0,6)));
// }

//Stuff(); 
//BenchmarkRunner.Run(typeof(Program).Assembly);
var s = new StringTest();
Console.Write("Using HashSET.....");
s.UsingHashSet();
Console.WriteLine(Environment.NewLine);
Console.Write("Using Regex.....");
s.UsingRegex();
Console.ReadLine();



