using Hangfire.Logging;

namespace Solhigson.Framework.Logging.Hangfire
{
    public class HangfireLogProvider : ILogProvider
    {
        private readonly HangfireLogger _provider;

        public HangfireLogProvider()
        {
            _provider = new HangfireLogger();
        }

        public ILog GetLogger(string name)
        {
            return _provider;
        }
    }
}