using System;
using System.Collections.Generic;

namespace Solhigson.Framework.Infrastructure
{
    public class SolhigsonAppSettings
    {
        private readonly ConfigurationWrapper _configurationWrapper;

        public SolhigsonAppSettings(ConfigurationWrapper configurationWrapper)
        {
            _configurationWrapper = configurationWrapper;
        }
        public string ProtectedFields => _configurationWrapper.GetConfig("Solhigson.Framework", "ProtectedFields", "");
        
        #region Smtp

        private T GetSmtpConfig<T>(string config, string defaultValue = null)
        {
            return _configurationWrapper.GetConfig<T>("Smtp", config, defaultValue);
        }

        public string SmtpServer => GetSmtpConfig<string>("Server");
        public int SmtpPort => GetSmtpConfig<int>("Port");
        public string SmtpUsername => GetSmtpConfig<string>("Username", "");
        public string SmtpPassword => GetSmtpConfig<string>("Password", "");

        public bool SmtpEnableSsl => GetSmtpConfig<bool>("EnableSsl", "true");
        #endregion


    }
}