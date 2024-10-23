using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging.Nlog.Renderers;

namespace Solhigson.Framework.Logging;

public class LogWrapper
{
    internal LogWrapper(string? name)
    {
        InternalLogger = NLog.LogManager.GetLogger(name);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return InternalLogger.IsEnabled(logLevel);
    }

    public Logger InternalLogger { get; }

    public bool IsDebugEnabled => InternalLogger.IsDebugEnabled;

    internal void Log(string? message, LogLevel logLevel, object? data = null,
        Exception? exception = null, string? serviceName = null, string? serviceType = null,
        string group = Constants.Group.AppLog, string? status = null, string? endPointUrl = null,
        string? chainId = null)
    {
        try
        {
            if (exception is TaskCanceledException or OperationCanceledException)
            {
                var configurationWrapper = ServiceProviderWrapper.ServiceProvider.GetService<ConfigurationWrapper>();
                if (configurationWrapper is not null)
                {
                    if (configurationWrapper.GetConfig<bool>("appSettings", "IgnoreTaskCancelledException", "false"))
                    {
                        return;
                    }
                }
            }
        }
        catch
        {
            //
        }

        if (!InternalLogger.IsEnabled(logLevel))
        {
            return;
        }

        var eventInfo = LogEventInfo.Create(logLevel, InternalLogger.Name, exception, CultureInfo.InvariantCulture,
            message);
        //eventInfo.Exception = exception;
        eventInfo.TimeStamp = DateTime.UtcNow;
        eventInfo.Properties[CustomDataRenderer.Name] = data;
        eventInfo.Properties["serviceName"] = serviceName;
        eventInfo.Properties["serviceType"] = serviceType;
        eventInfo.Properties[GroupRenderer.Name] = group;
        eventInfo.Properties["status"] = status;
        eventInfo.Properties["url"] = endPointUrl;
        eventInfo.Properties[UserRenderer.Name] = ServiceProviderWrapper.GetHttpContextAccessor()?.GetEmailClaim() ??
                                                  ServiceProviderWrapper.GetCurrentLogUserEmail();
        eventInfo.Properties["chainId"] = chainId ?? ServiceProviderWrapper.GetCurrentLogChainId();
        InternalLogger.Log(eventInfo);
    }

    private void Log(LogLevel logLevel, string message, Exception? exception, params object?[]? args)
    {
        try
        {
            if (exception is TaskCanceledException or OperationCanceledException)
            {
                var configurationWrapper = ServiceProviderWrapper.ServiceProvider.GetService<ConfigurationWrapper>();
                if (configurationWrapper is not null)
                {
                    if (configurationWrapper.GetConfig<bool>("appSettings", "IgnoreTaskCancelledException", "false"))
                    {
                        return;
                    }
                }
            }
        }
        catch
        {
            //
        }

        if (!InternalLogger.IsEnabled(logLevel))
        {
            return;
        }

        // var vals = new FormattedLogValues(message, args);

        // var eventInfo = LogEventInfo.Create(logLevel, InternalLogger.Name, exception, CultureInfo.InvariantCulture, vals.ToString());
        var eventInfo = LogEventInfo.Create(logLevel, InternalLogger.Name, exception, CultureInfo.InvariantCulture, message, args);
        eventInfo.TimeStamp = DateTime.UtcNow;
        eventInfo.Properties[UserRenderer.Name] = ServiceProviderWrapper.GetHttpContextAccessor()?.GetEmailClaim() ??
                                                  ServiceProviderWrapper.GetCurrentLogUserEmail();
        eventInfo.Properties["chainId"] = ServiceProviderWrapper.GetCurrentLogChainId();
        InternalLogger.Log(eventInfo);
    }


    [Obsolete("This will be depreciated in future releases, use LogDebug() instead")]
    public void Debug(string message, object? data = null)
    {
        if (InternalLogger.IsDebugEnabled)
        {
            Log(message, LogLevel.Debug, data);
        }
    }

    [MessageTemplateFormatMethod("message")]
    public void LogDebug(string message, params object?[] args)
    {
        if (!InternalLogger.IsDebugEnabled)
        {
            return;
        }

        var chainId = ServiceProviderWrapper.GetCurrentLogChainId();
        if (!string.IsNullOrWhiteSpace(chainId))
        {
            message += " {chain}";
            Log(LogLevel.Debug, message, null, Combine(args, chainId));
            return;
        }

        Log(LogLevel.Debug, message, null, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogInformation() instead")]
    public void Info(string message, object? data = null)
    {
        if (InternalLogger.IsInfoEnabled)
        {
            Log(message, LogLevel.Info, data);
        }
    }

    [MessageTemplateFormatMethod("message")]
    public void LogInformation(string message, params object?[] args)
    {
        if (!InternalLogger.IsInfoEnabled)
        {
            return;
        }

        var chainId = ServiceProviderWrapper.GetCurrentLogChainId();
        if (!string.IsNullOrWhiteSpace(chainId))
        {
            message += " {chain}";
            Log(LogLevel.Info, message, null, Combine(args, chainId));
            return;
        }

        Log(LogLevel.Info, message, null, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogWarn() instead")]
    public void Warn(string message, object? data = null)
    {
        if (InternalLogger.IsWarnEnabled)
        {
            Log(message, LogLevel.Warn, data);
        }
    }

    [MessageTemplateFormatMethod("message")]
    public void LogWarn(string message, params object?[] args)
    {
        if (!InternalLogger.IsWarnEnabled)
        {
            return;
        }

        var chainId = ServiceProviderWrapper.GetCurrentLogChainId();
        if (!string.IsNullOrWhiteSpace(chainId))
        {
            message += " {chain}";
            Log(LogLevel.Warn, message, null, Combine(args, chainId));
            return;
        }

        Log(LogLevel.Warn, message, null, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogError) instead")]
    public void Error(Exception e, string? message = null, object? data = null)
    {
        if (InternalLogger.IsErrorEnabled)
        {
            Log(message, LogLevel.Error, data, e);
        }
    }

    [MessageTemplateFormatMethod("message")]
    public void LogError(Exception e, string? message = null, params object?[] args)
    {
        if (!InternalLogger.IsErrorEnabled)
        {
            return;
        }

        message ??= e.Message;
        var chainId = ServiceProviderWrapper.GetCurrentLogChainId();
        if (!string.IsNullOrWhiteSpace(chainId))
        {
            message += " {chain}";
            Log(LogLevel.Error, message, e, Combine(args, chainId));
            return;
        }

        Log(LogLevel.Error, message, e, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogFatal() instead")]
    public void Fatal(string message, Exception? e = null, object? data = null)
    {
        if (InternalLogger.IsFatalEnabled)
        {
            Log(message, LogLevel.Fatal, data, e);
        }
    }

    [MessageTemplateFormatMethod("message")]
    public void LogFatal(Exception e, string? message = null, params object?[] args)
    {
        if (!InternalLogger.IsFatalEnabled)
        {
            return;
        }

        message ??= e.Message;
        var chainId = ServiceProviderWrapper.GetCurrentLogChainId();
        if (!string.IsNullOrWhiteSpace(chainId))
        {
            message += " {chain}";
            Log(LogLevel.Fatal, message, e, Combine(args, chainId));
            return;
        }

        Log(LogLevel.Fatal, message, e, args);
    }


    [Obsolete("This will be depreciated in future releases, use LogTrace() instead")]
    public void Trace(string message, object? data = null)
    {
        if (InternalLogger.IsTraceEnabled)
        {
            Log(message, LogLevel.Trace, data);
        }
    }

    [MessageTemplateFormatMethod("message")]
    public void LogTrace(string message, params object?[] args)
    {
        if (!InternalLogger.IsTraceEnabled)
        {
            return;
        }

        var chainId = ServiceProviderWrapper.GetCurrentLogChainId();
        if (!string.IsNullOrWhiteSpace(chainId))
        {
            message += " {chain}";
            Log(LogLevel.Trace, message, null, Combine(args, chainId));
            return;
        }

        Log(LogLevel.Trace, message, null, args);
    }

    private static object?[]? Combine(object?[]? args, params object?[]? otherArgs)
    {
        if (args is null || args.Length == 0)
        {
            return otherArgs;
        }

        if (otherArgs is null || otherArgs.Length == 0)
        {
            return args;
        }

        var result = new object[args.Length + otherArgs.Length];
        Array.Copy(args, result, args.Length);
        Array.Copy(otherArgs, 0, result, args.Length, otherArgs.Length);
        return result;
    }
}

// internal readonly struct FormattedLogValues : IReadOnlyList<KeyValuePair<string, object?>>
// {
//     internal const int MaxCachedFormatters = 1024;
//     private const string NullFormat = "[null]";
//     private static int _count;
//
//     private static ConcurrentDictionary<string, LogValuesFormatter> _formatters =
//         new ConcurrentDictionary<string, LogValuesFormatter>();
//
//     private readonly LogValuesFormatter? _formatter;
//     private readonly object?[]? _values;
//     private readonly string _originalMessage;
//
//     // for testing purposes
//     internal LogValuesFormatter? Formatter => _formatter;
//
//     public FormattedLogValues(string? format, params object?[]? values)
//     {
//         if (values != null && values.Length != 0 && format != null)
//         {
//             if (_count >= MaxCachedFormatters)
//             {
//                 if (!_formatters.TryGetValue(format, out _formatter))
//                 {
//                     _formatter = new LogValuesFormatter(format);
//                 }
//             }
//             else
//             {
//                 _formatter = _formatters.GetOrAdd(format, f =>
//                 {
//                     Interlocked.Increment(ref _count);
//                     return new LogValuesFormatter(f);
//                 });
//             }
//         }
//         else
//         {
//             _formatter = null;
//         }
//
//         _originalMessage = format ?? NullFormat;
//         _values = values;
//     }
//
//     public KeyValuePair<string, object?> this[int index]
//     {
//         get
//         {
//             if (index < 0 || index >= Count)
//             {
//                 throw new IndexOutOfRangeException(nameof(index));
//             }
//
//             if (index == Count - 1)
//             {
//                 return new KeyValuePair<string, object?>("{OriginalFormat}", _originalMessage);
//             }
//
//             return _formatter!.GetValue(_values!, index);
//         }
//     }
//
//     public int Count
//     {
//         get
//         {
//             if (_formatter == null)
//             {
//                 return 1;
//             }
//
//             return _formatter.ValueNames.Count + 1;
//         }
//     }
//
//     public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
//     {
//         for (int i = 0; i < Count; ++i)
//         {
//             yield return this[i];
//         }
//     }
//
//     public override string ToString()
//     {
//         if (_formatter == null)
//         {
//             return _originalMessage;
//         }
//
//         return _formatter.Format(_values);
//     }
//
//     IEnumerator IEnumerable.GetEnumerator()
//     {
//         return GetEnumerator();
//     }
// }
//
// internal sealed class LogValuesFormatter
// {
//     private const string NullValue = "(null)";
//     private static readonly char[] FormatDelimiters = { ',', ':' };
//     private readonly string _format;
//     private readonly List<string> _valueNames = new List<string>();
//
//     // NOTE: If this assembly ever builds for netcoreapp, the below code should change to:
//     // - Be annotated as [SkipLocalsInit] to avoid zero'ing the stackalloc'd char span
//     // - Format _valueNames.Count directly into a span
//
//     
//     public LogValuesFormatter(string format)
//     {
//         if (format == null)
//         {
//             throw new ArgumentNullException(nameof(format));
//         }
//
//         OriginalFormat = format;
//
//         var vsb = new ValueStringBuilder(stackalloc char[256]);
//         int scanIndex = 0;
//         int endIndex = format.Length;
//
//         while (scanIndex < endIndex)
//         {
//             int openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
//             if (scanIndex == 0 && openBraceIndex == endIndex)
//             {
//                 // No holes found.
//                 _format = format;
//                 return;
//             }
//
//             int closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);
//
//             if (closeBraceIndex == endIndex)
//             {
//                 vsb.Append(format.AsSpan(scanIndex, endIndex - scanIndex));
//                 scanIndex = endIndex;
//             }
//             else
//             {
//                 // Format item syntax : { index[,alignment][ :formatString] }.
//                 int formatDelimiterIndex = FindIndexOfAny(format, FormatDelimiters, openBraceIndex, closeBraceIndex);
//
//                 vsb.Append(format.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
//                 vsb.Append(_valueNames.Count.ToString());
//                 _valueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
//                 vsb.Append(format.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1));
//
//                 scanIndex = closeBraceIndex + 1;
//             }
//         }
//
//         _format = vsb.ToString();
//     }
//
//     public string OriginalFormat { get; private set; }
//     public List<string> ValueNames => _valueNames;
//
//     private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
//     {
//         // Example: {{prefix{{{Argument}}}suffix}}.
//         int braceIndex = endIndex;
//         int scanIndex = startIndex;
//         int braceOccurrenceCount = 0;
//
//         while (scanIndex < endIndex)
//         {
//             if (braceOccurrenceCount > 0 && format[scanIndex] != brace)
//             {
//                 if (braceOccurrenceCount % 2 == 0)
//                 {
//                     // Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'.
//                     braceOccurrenceCount = 0;
//                     braceIndex = endIndex;
//                 }
//                 else
//                 {
//                     // An unescaped '{' or '}' found.
//                     break;
//                 }
//             }
//             else if (format[scanIndex] == brace)
//             {
//                 if (brace == '}')
//                 {
//                     if (braceOccurrenceCount == 0)
//                     {
//                         // For '}' pick the first occurrence.
//                         braceIndex = scanIndex;
//                     }
//                 }
//                 else
//                 {
//                     // For '{' pick the last occurrence.
//                     braceIndex = scanIndex;
//                 }
//
//                 braceOccurrenceCount++;
//             }
//
//             scanIndex++;
//         }
//
//         return braceIndex;
//     }
//
//     private static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
//     {
//         int findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
//         return findIndex == -1 ? endIndex : findIndex;
//     }
//
//     public string Format(object?[]? values)
//     {
//         object?[]? formattedValues = values;
//
//         if (values != null)
//         {
//             for (int i = 0; i < values.Length; i++)
//             {
//                 object formattedValue = FormatArgument(values[i]);
//                 // If the formatted value is changed, we allocate and copy items to a new array to avoid mutating the array passed in to this method
//                 if (!ReferenceEquals(formattedValue, values[i]))
//                 {
//                     formattedValues = new object[values.Length];
//                     Array.Copy(values, formattedValues, i);
//                     formattedValues[i++] = formattedValue;
//                     for (; i < values.Length; i++)
//                     {
//                         formattedValues[i] = FormatArgument(values[i]);
//                     }
//
//                     break;
//                 }
//             }
//         }
//
//         return string.Format(CultureInfo.InvariantCulture, _format, formattedValues ?? Array.Empty<object>());
//     }
//
//     // NOTE: This method mutates the items in the array if needed to avoid extra allocations, and should only be used when caller expects this to happen
//     internal string FormatWithOverwrite(object?[]? values)
//     {
//         if (values != null)
//         {
//             for (int i = 0; i < values.Length; i++)
//             {
//                 values[i] = FormatArgument(values[i]);
//             }
//         }
//
//         return string.Format(CultureInfo.InvariantCulture, _format, values ?? Array.Empty<object>());
//     }
//
//     internal string Format()
//     {
//         return _format;
//     }
//
//     internal string Format(object? arg0)
//     {
//         return string.Format(CultureInfo.InvariantCulture, _format, FormatArgument(arg0));
//     }
//
//     internal string Format(object? arg0, object? arg1)
//     {
//         return string.Format(CultureInfo.InvariantCulture, _format, FormatArgument(arg0), FormatArgument(arg1));
//     }
//
//     internal string Format(object? arg0, object? arg1, object? arg2)
//     {
//         return string.Format(CultureInfo.InvariantCulture, _format, FormatArgument(arg0), FormatArgument(arg1),
//             FormatArgument(arg2));
//     }
//
//     public KeyValuePair<string, object?> GetValue(object?[] values, int index)
//     {
//         if (index < 0 || index > _valueNames.Count)
//         {
//             throw new IndexOutOfRangeException(nameof(index));
//         }
//
//         if (_valueNames.Count > index)
//         {
//             return new KeyValuePair<string, object?>(_valueNames[index], values[index]);
//         }
//
//         return new KeyValuePair<string, object?>("{OriginalFormat}", OriginalFormat);
//     }
//
//     public IEnumerable<KeyValuePair<string, object?>> GetValues(object[] values)
//     {
//         var valueArray = new KeyValuePair<string, object?>[values.Length + 1];
//         for (int index = 0; index != _valueNames.Count; ++index)
//         {
//             valueArray[index] = new KeyValuePair<string, object?>(_valueNames[index], values[index]);
//         }
//
//         valueArray[valueArray.Length - 1] = new KeyValuePair<string, object?>("{OriginalFormat}", OriginalFormat);
//         return valueArray;
//     }
//
//     private object FormatArgument(object? value)
//     {
//         if (value == null)
//         {
//             return NullValue;
//         }
//
//         // since 'string' implements IEnumerable, special case it
//         if (value is string)
//         {
//             return value;
//         }
//
//         // if the value implements IEnumerable, build a comma separated string.
//         if (value is IEnumerable enumerable)
//         {
//             var vsb = new ValueStringBuilder(stackalloc char[256]);
//             bool first = true;
//             foreach (object? e in enumerable)
//             {
//                 if (!first)
//                 {
//                     vsb.Append(", ");
//                 }
//
//                 vsb.Append(e != null ? e.ToString() : NullValue);
//                 first = false;
//             }
//
//             return vsb.ToString();
//         }
//
//         return value;
//     }
// }
//
// internal ref partial struct ValueStringBuilder
// {
//     private char[]? _arrayToReturnToPool;
//     private Span<char> _chars;
//     private int _pos;
//
//     public ValueStringBuilder(Span<char> initialBuffer)
//     {
//         _arrayToReturnToPool = null;
//         _chars = initialBuffer;
//         _pos = 0;
//     }
//
//     public ValueStringBuilder(int initialCapacity)
//     {
//         _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
//         _chars = _arrayToReturnToPool;
//         _pos = 0;
//     }
//
//     public int Length
//     {
//         get => _pos;
//         set
//         {
//             Debug.Assert(value >= 0);
//             Debug.Assert(value <= _chars.Length);
//             _pos = value;
//         }
//     }
//
//     public int Capacity => _chars.Length;
//
//     public void EnsureCapacity(int capacity)
//     {
//         // This is not expected to be called this with negative capacity
//         Debug.Assert(capacity >= 0);
//
//         // If the caller has a bug and calls this with negative capacity, make sure to call Grow to throw an exception.
//         if ((uint)capacity > (uint)_chars.Length)
//             Grow(capacity - _pos);
//     }
//
//     /// <summary>
//     /// Get a pinnable reference to the builder.
//     /// Does not ensure there is a null char after <see cref="Length"/>
//     /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
//     /// the explicit method call, and write eg "fixed (char* c = builder)"
//     /// </summary>
//     public ref char GetPinnableReference()
//     {
//         return ref MemoryMarshal.GetReference(_chars);
//     }
//
//     /// <summary>
//     /// Get a pinnable reference to the builder.
//     /// </summary>
//     /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
//     public ref char GetPinnableReference(bool terminate)
//     {
//         if (terminate)
//         {
//             EnsureCapacity(Length + 1);
//             _chars[Length] = '\0';
//         }
//
//         return ref MemoryMarshal.GetReference(_chars);
//     }
//
//     public ref char this[int index]
//     {
//         get
//         {
//             Debug.Assert(index < _pos);
//             return ref _chars[index];
//         }
//     }
//
//     public override string ToString()
//     {
//         string s = _chars.Slice(0, _pos).ToString();
//         Dispose();
//         return s;
//     }
//
//     /// <summary>Returns the underlying storage of the builder.</summary>
//     public Span<char> RawChars => _chars;
//
//     /// <summary>
//     /// Returns a span around the contents of the builder.
//     /// </summary>
//     /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
//     public ReadOnlySpan<char> AsSpan(bool terminate)
//     {
//         if (terminate)
//         {
//             EnsureCapacity(Length + 1);
//             _chars[Length] = '\0';
//         }
//
//         return _chars.Slice(0, _pos);
//     }
//
//     public ReadOnlySpan<char> AsSpan() => _chars.Slice(0, _pos);
//     public ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, _pos - start);
//     public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);
//
//     public bool TryCopyTo(Span<char> destination, out int charsWritten)
//     {
//         if (_chars.Slice(0, _pos).TryCopyTo(destination))
//         {
//             charsWritten = _pos;
//             Dispose();
//             return true;
//         }
//         else
//         {
//             charsWritten = 0;
//             Dispose();
//             return false;
//         }
//     }
//
//     public void Insert(int index, char value, int count)
//     {
//         if (_pos > _chars.Length - count)
//         {
//             Grow(count);
//         }
//
//         int remaining = _pos - index;
//         _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
//         _chars.Slice(index, count).Fill(value);
//         _pos += count;
//     }
//
//     public void Insert(int index, string? s)
//     {
//         if (s == null)
//         {
//             return;
//         }
//
//         int count = s.Length;
//
//         if (_pos > (_chars.Length - count))
//         {
//             Grow(count);
//         }
//
//         int remaining = _pos - index;
//         _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
//         s
// #if !NET6_0_OR_GREATER
//                 .AsSpan()
// #endif
//             .CopyTo(_chars.Slice(index));
//         _pos += count;
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public void Append(char c)
//     {
//         int pos = _pos;
//         if ((uint)pos < (uint)_chars.Length)
//         {
//             _chars[pos] = c;
//             _pos = pos + 1;
//         }
//         else
//         {
//             GrowAndAppend(c);
//         }
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public void Append(string? s)
//     {
//         if (s == null)
//         {
//             return;
//         }
//
//         int pos = _pos;
//         if (s.Length == 1 &&
//             (uint)pos < (uint)_chars
//                 .Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
//         {
//             _chars[pos] = s[0];
//             _pos = pos + 1;
//         }
//         else
//         {
//             AppendSlow(s);
//         }
//     }
//
//     private void AppendSlow(string s)
//     {
//         int pos = _pos;
//         if (pos > _chars.Length - s.Length)
//         {
//             Grow(s.Length);
//         }
//
//         s
// #if !NET6_0_OR_GREATER
//                 .AsSpan()
// #endif
//             .CopyTo(_chars.Slice(pos));
//         _pos += s.Length;
//     }
//
//     public void Append(char c, int count)
//     {
//         if (_pos > _chars.Length - count)
//         {
//             Grow(count);
//         }
//
//         Span<char> dst = _chars.Slice(_pos, count);
//         for (int i = 0; i < dst.Length; i++)
//         {
//             dst[i] = c;
//         }
//
//         _pos += count;
//     }
//
//     // public unsafe void Append(char* value, int length)
//     // {
//     //     int pos = _pos;
//     //     if (pos > _chars.Length - length)
//     //     {
//     //         Grow(length);
//     //     }
//     //
//     //     Span<char> dst = _chars.Slice(_pos, length);
//     //     for (int i = 0; i < dst.Length; i++)
//     //     {
//     //         dst[i] = *value++;
//     //     }
//     //     _pos += length;
//     // }
//
//     public void Append(ReadOnlySpan<char> value)
//     {
//         int pos = _pos;
//         if (pos > _chars.Length - value.Length)
//         {
//             Grow(value.Length);
//         }
//
//         value.CopyTo(_chars.Slice(_pos));
//         _pos += value.Length;
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public Span<char> AppendSpan(int length)
//     {
//         int origPos = _pos;
//         if (origPos > _chars.Length - length)
//         {
//             Grow(length);
//         }
//
//         _pos = origPos + length;
//         return _chars.Slice(origPos, length);
//     }
//
//     [MethodImpl(MethodImplOptions.NoInlining)]
//     private void GrowAndAppend(char c)
//     {
//         Grow(1);
//         Append(c);
//     }
//
//     /// <summary>
//     /// Resize the internal buffer either by doubling current buffer size or
//     /// by adding <paramref name="additionalCapacityBeyondPos"/> to
//     /// <see cref="_pos"/> whichever is greater.
//     /// </summary>
//     /// <param name="additionalCapacityBeyondPos">
//     /// Number of chars requested beyond current position.
//     /// </param>
//     [MethodImpl(MethodImplOptions.NoInlining)]
//     private void Grow(int additionalCapacityBeyondPos)
//     {
//         Debug.Assert(additionalCapacityBeyondPos > 0);
//         Debug.Assert(_pos > _chars.Length - additionalCapacityBeyondPos,
//             "Grow called incorrectly, no resize is needed.");
//
//         // Make sure to let Rent throw an exception if the caller has a bug and the desired capacity is negative
//         char[] poolArray =
//             ArrayPool<char>.Shared.Rent((int)Math.Max((uint)(_pos + additionalCapacityBeyondPos),
//                 (uint)_chars.Length * 2));
//
//         _chars.Slice(0, _pos).CopyTo(poolArray);
//
//         char[]? toReturn = _arrayToReturnToPool;
//         _chars = _arrayToReturnToPool = poolArray;
//         if (toReturn != null)
//         {
//             ArrayPool<char>.Shared.Return(toReturn);
//         }
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public void Dispose()
//     {
//         char[]? toReturn = _arrayToReturnToPool;
//         this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
//         if (toReturn != null)
//         {
//             ArrayPool<char>.Shared.Return(toReturn);
//         }
//     }
// }