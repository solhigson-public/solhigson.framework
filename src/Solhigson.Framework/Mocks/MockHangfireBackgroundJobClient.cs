using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Mocks
{
    public class MockHangfireBackgroundJobClient : IBackgroundJobClient
    {
        private readonly ILifetimeScope _lifetimeScope;
        public MockHangfireBackgroundJobClient(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }
        
        public string Create(Job job, IState state)
        {
            try
            {
                if (!job.Type.IsAbstract)
                {
                    var obj = _lifetimeScope.Resolve(job.Type);
                    var result = job.Method.Invoke(obj, job.Args.ToArray());
                    if (result is Task task) //for async methods
                    {
                        task.Wait();
                    }
                }
            }
            catch(Exception e)
            {
                this.ELogError(e);
            }
            return Guid.NewGuid().ToString();
        }

        public bool ChangeState(string jobId, IState state, string expectedState)
        {
            return true;
        }
    }
}