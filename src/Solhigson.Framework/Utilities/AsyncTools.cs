using System;
using System.Threading;
using System.Threading.Tasks;

namespace Solhigson.Framework.Utilities;

public class AsyncTools
{
    private static readonly Lazy<TaskFactory> TaskFactory = new(() => new TaskFactory(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default));
    private static TaskFactory MyTaskFactory => TaskFactory.Value;

    /// <summary>
    /// Run task sync.
    /// </summary>
    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        return MyTaskFactory.StartNew(func).Unwrap().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Run sync.
    /// </summary>
    public static void RunSync(Func<Task> func)
    {
        MyTaskFactory.StartNew(func).Unwrap().GetAwaiter().GetResult();
    }
}