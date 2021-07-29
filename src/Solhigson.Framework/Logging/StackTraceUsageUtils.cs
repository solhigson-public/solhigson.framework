using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog.Config;

namespace Solhigson.Framework.Logging
{
    internal static class StackTraceUsageUtils
    {
        private static readonly Assembly NlogAssembly = typeof(StackTraceUsageUtils).Assembly;
        private static readonly Assembly MscorlibAssembly = typeof(string).Assembly;
        private static readonly Assembly SystemAssembly = typeof(Debug).Assembly;

        internal static StackTraceUsage Max(StackTraceUsage u1, StackTraceUsage u2)
        {
            return (StackTraceUsage) Math.Max((int) u1, (int) u2);
        }

        public static int GetFrameCount(this StackTrace stackTrace)
        {
#if !NETSTANDARD1_0
            return stackTrace.FrameCount;
#else
            return strackTrace.GetFrames().Length;
#endif
        }

        public static string GetStackFrameMethodName(MethodBase method, bool includeMethodInfo, bool cleanAsyncMoveNext,
            bool cleanAnonymousDelegates)
        {
            if (method == null)
                return null;

            var methodName = method.Name;

            var callerClassType = method.DeclaringType;
            if (cleanAsyncMoveNext && methodName == "MoveNext" && callerClassType?.DeclaringType != null &&
                callerClassType.Name.StartsWith("<"))
            {
                // NLog.UnitTests.LayoutRenderers.CallSiteTests+<CleanNamesOfAsyncContinuations>d_3'1.MoveNext
                var endIndex = callerClassType.Name.IndexOf('>', 1);
                if (endIndex > 1)
                {
                    methodName = callerClassType.Name.Substring(1, endIndex - 1);
                    if (methodName.StartsWith("<"))
                        methodName =
                            methodName.Substring(1,
                                methodName.Length - 1); // Local functions, and anonymous-methods in Task.Run()
                }
            }

            // Clean up the function name if it is an anonymous delegate
            // <.ctor>b__0
            // <Main>b__2
            if (cleanAnonymousDelegates && methodName.StartsWith("<") && methodName.Contains("__") &&
                methodName.Contains(">"))
            {
                var startIndex = methodName.IndexOf('<') + 1;
                var endIndex = methodName.IndexOf('>');

                methodName = methodName.Substring(startIndex, endIndex - startIndex);
            }

            if (includeMethodInfo && methodName == method.Name) methodName = method.ToString();

            return methodName;
        }

        public static string GetStackFrameMethodClassName(MethodBase method, bool includeNameSpace,
            bool cleanAsyncMoveNext, bool cleanAnonymousDelegates)
        {
            if (method == null)
                return null;

            var callerClassType = method.DeclaringType;
            if (cleanAsyncMoveNext && method.Name == "MoveNext" && callerClassType?.DeclaringType != null &&
                callerClassType.Name.StartsWith("<"))
            {
                // NLog.UnitTests.LayoutRenderers.CallSiteTests+<CleanNamesOfAsyncContinuations>d_3'1
                var endIndex = callerClassType.Name.IndexOf('>', 1);
                if (endIndex > 1) callerClassType = callerClassType.DeclaringType;
            }

            if (!includeNameSpace
                && callerClassType?.DeclaringType != null
                && callerClassType.IsNested
                && callerClassType.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                return callerClassType.DeclaringType.Name;

            var className = includeNameSpace ? callerClassType?.FullName : callerClassType?.Name;

            if (cleanAnonymousDelegates && className != null)
            {
                // NLog.UnitTests.LayoutRenderers.CallSiteTests+<>c__DisplayClassa
                var index = className.IndexOf("+<>", StringComparison.Ordinal);
                if (index >= 0) className = className.Substring(0, index);
            }

            return className;
        }

        /// <summary>
        ///     Gets the fully qualified name of the class invoking the calling method, including the
        ///     namespace but not the assembly.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetClassFullName()
        {
            var framesToSkip = 2;

            var className = string.Empty;
#if SILVERLIGHT
            var stackFrame = new StackFrame(framesToSkip);
            className = GetClassFullName(stackFrame);
#elif !NETSTANDARD1_0
            var stackFrame = new StackFrame(framesToSkip, false);
            className = GetClassFullName(stackFrame);
#else
            var stackTrace = Environment.StackTrace;
            var stackTraceLines = stackTrace.Replace("\r", "").SplitAndTrimTokens('\n');
            for (int i = 0; i < stackTraceLines.Length; ++i)
            {
                var callingClassAndMethod =
 stackTraceLines[i].Split(new[] { " ", "<>", "(", ")" }, StringSplitOptions.RemoveEmptyEntries)[1];
                int methodStartIndex = callingClassAndMethod.LastIndexOf(".", StringComparison.Ordinal);
                if (methodStartIndex > 0)
                {
                    // Trim method name. 
                    var callingClass = callingClassAndMethod.Substring(0, methodStartIndex);
                    // Needed because of extra dot, for example if method was .ctor()
                    className = callingClass.TrimEnd('.');
                    if (!className.StartsWith("System.Environment") && framesToSkip != 0)
                    {
                        i += framesToSkip - 1;
                        framesToSkip = 0;
                        continue;
                    }
                    if (!className.StartsWith("System."))
                        break;
                }
            }
#endif
            return className;
        }

#if !NETSTANDARD1_0
        /// <summary>
        ///     Gets the fully qualified name of the class invoking the calling method, including the
        ///     namespace but not the assembly.
        /// </summary>
        /// <param name="stackFrame">StackFrame from the calling method</param>
        /// <returns>Fully qualified class name</returns>
        public static string GetClassFullName(StackFrame stackFrame)
        {
            var className = LookupClassNameFromStackFrame(stackFrame);
            if (string.IsNullOrEmpty(className))
            {
#if SILVERLIGHT
                var stackTrace = new StackTrace();
#else
                var stackTrace = new StackTrace(false);
#endif
                className = GetClassFullName(stackTrace);
                if (string.IsNullOrEmpty(className))
                    className = stackFrame.GetMethod()?.Name ?? string.Empty;
            }

            return className;
        }
#endif

        private static string GetClassFullName(StackTrace stackTrace)
        {
            foreach (var frame in stackTrace.GetFrames())
            {
                var className = LookupClassNameFromStackFrame(frame);
                if (!string.IsNullOrEmpty(className)) return className;
            }

            return string.Empty;
        }

        /// <summary>
        ///     Returns the assembly from the provided StackFrame (If not internal assembly)
        /// </summary>
        /// <returns>Valid assembly, or null if assembly was internal</returns>
        public static Assembly LookupAssemblyFromStackFrame(StackFrame stackFrame)
        {
            var method = stackFrame.GetMethod();
            if (method == null) return null;

            var assembly = method.DeclaringType?.Assembly ?? method.Module?.Assembly;
            // skip stack frame if the method declaring type assembly is from hidden assemblies list
            if (assembly == NlogAssembly) return null;

            if (assembly == MscorlibAssembly) return null;

            if (assembly == SystemAssembly) return null;

            return assembly;
        }

        /// <summary>
        ///     Returns the classname from the provided StackFrame (If not from internal assembly)
        /// </summary>
        /// <param name="stackFrame"></param>
        /// <returns>Valid class name, or empty string if assembly was internal</returns>
        public static string LookupClassNameFromStackFrame(StackFrame stackFrame)
        {
            var method = stackFrame.GetMethod();
            if (method != null && LookupAssemblyFromStackFrame(stackFrame) != null)
            {
                var className = GetStackFrameMethodClassName(method, true, true, true);
                if (!string.IsNullOrEmpty(className))
                {
                    if (!className.StartsWith("System.", StringComparison.Ordinal))
                        return className;
                }
                else
                {
                    className = method.Name ?? string.Empty;
                    if (className != "lambda_method" && className != "MoveNext")
                        return className;
                }
            }

            return string.Empty;
        }
    }
}