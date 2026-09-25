using Microsoft.Extensions.Logging;
using Solhigson.Framework.Logging;

namespace Solhigson.Framework.AuditCapture;

/// <summary>
/// Resolves the audit-capture logger AT CALL TIME against the factory installed on <see cref="LogManager"/> at that
/// moment. A <c>static readonly LogWrapper</c> field is NOT safe here: <see cref="LogWrapper"/> captures its
/// <see cref="ILogger"/> in its constructor, and <see cref="LogManager.GetLogger(string?, ILoggerFactory?)"/> caches
/// one wrapper per NAME forever, so a wrapper first requested before <see cref="LogManager.SetLoggerFactory"/> (a
/// consumer that runs migrations through the interceptor-bearing context at startup) stays null-backed for the
/// process lifetime and every warning or error it carries goes to NLog's internal logger instead of the app's
/// targets. Building the wrapper directly (its constructor is assembly-internal) bypasses that name cache.
/// </summary>
internal static class AuditCaptureLog
{
    /// <summary>The logger category, unchanged from the former static field.</summary>
    internal const string LoggerName = nameof(AuditCaptureSaveChangesInterceptor);

    /// <summary>The factory <see cref="LogManager"/> holds right now, or null when none is installed yet.</summary>
    internal static ILoggerFactory? CurrentLoggerFactory => LogManager.CurrentLoggerFactory;

    /// <summary>Null-backed wrapper (routes to NLog's internal logger); its constructor cannot throw.</summary>
    private static readonly LogWrapper Unbound = new(LoggerName, null);

    /// <summary>
    /// NEVER throws (never-block): binds a wrapper to the CURRENT factory. <see cref="LogWrapper"/>'s constructor
    /// calls <c>CreateLogger</c> unguarded, and an installed factory can throw there (e.g. a disposed
    /// <c>LoggerFactory</c> raises <see cref="System.ObjectDisposedException"/>). Returns false, with the
    /// null-backed wrapper, when no factory is installed or binding threw.
    /// </summary>
    internal static bool TryCreateBound(out LogWrapper logger)
    {
        try
        {
            if (CurrentLoggerFactory is { } factory)
            {
                logger = new LogWrapper(LoggerName, factory);
                return true;
            }
        }
        catch (System.Exception)
        {
            // Swallowed by design: logging must never block a save, and a broken factory has nowhere to report.
        }

        logger = Unbound;
        return false;
    }

    /// <summary>
    /// NEVER throws: a wrapper bound to the CURRENT factory, or the null-backed wrapper when none is installed or
    /// binding failed. Emission itself is guarded inside <see cref="LogWrapper"/>. Every capture, handoff and
    /// failure path logs through this.
    /// </summary>
    internal static LogWrapper Current()
    {
        TryCreateBound(out var logger);
        return logger;
    }
}
