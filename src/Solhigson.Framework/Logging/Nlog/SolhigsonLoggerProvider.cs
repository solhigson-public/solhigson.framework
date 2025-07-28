using Solhigson.Framework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Solhigson.Framework.Logging.Nlog;

public class SolhigsonLoggerProvider(NLogLoggerProvider innerProvider) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new SolhigsonLogger(innerProvider.CreateLogger(categoryName));

    public void Dispose()
    {
        innerProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private class SolhigsonLogger(ILogger inner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => inner.BeginScope(state);

        public bool IsEnabled(LogLevel level)
            => inner.IsEnabled(level);

        public void Log<TState>(
            LogLevel level,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var customProperties = ServiceProviderWrapper.GetScopedProperties()?.Properties;

            if (customProperties is null)
            {
                inner.Log(level, eventId, state, exception, formatter);
                return;
            }


            // 2) If the incoming state is already a set of key/values, merge
            if (state is IEnumerable<KeyValuePair<string, object>> existing)
            {
                var merged = existing.ToDictionary(k => k.Key, v => v.Value);
                foreach (var kv in customProperties)
                    merged[kv.Key] = kv.Value;

                // Pass the merged dictionary as the new state
                inner.Log(
                    level, eventId,
                    merged,
                    exception,
                    (_, ex) => formatter(state, ex)
                );
                return;
            }

            // 3) Otherwise, just use the customProperties as the state
            inner.Log(
                level, eventId,
                customProperties,
                exception,
                (_, ex) => formatter(state, ex)
            );
        }
    }
}