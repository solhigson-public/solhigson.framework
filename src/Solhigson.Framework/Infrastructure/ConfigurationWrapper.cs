using System;
using Microsoft.Extensions.Configuration;

namespace Solhigson.Framework.Infrastructure
{
    public class ConfigurationWrapper
    {
        private readonly IConfiguration _configuration;

        public ConfigurationWrapper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetFromAppSettingFileOnly(string group, string key = null, string defaultValue = null)
        {
            return GetConfig(group, key, defaultValue, true);
        }

        public T GetConfig<T>(string groupName, string key = null, object defaultValue = null)
        {
            string val = null;
            if (defaultValue != null) val = defaultValue.ToString();
            var setting = GetConfig(groupName, key, val);
            return VerifySetting<T>(setting, key, groupName, defaultValue);
        }

        public string GetConfig(string group, string key = null, string defaultValue = null,
            bool useAppSettingsFileOnly = false)
        {
            var configKey = group;
            if (!string.IsNullOrWhiteSpace(key)) configKey += $":{key}";
            var value = _configuration[configKey];

            if (value != null) return value;

            if (!useAppSettingsFileOnly)
            {
            }

            return defaultValue ?? throw new Exception($"Configuration [{key}] for group [{group}] not found.");
        }

        private T VerifySetting<T>(string setting, string key, string groupName, object defaultValue = null)
        {
            try
            {
                return ChangeType<T>(key, setting, groupName);
            }
            catch (Exception e)
            {
                if (defaultValue == null) throw;
                this.ELogError(e,
                    $"Invalid value set for system setting, using default value of {defaultValue} instead.");
                return (T) defaultValue;
            }
        }

        private static T ChangeType<T>(string key, object value, string group)
        {
            try
            {
                return (T) Convert.ChangeType(value, typeof(T));
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"An invalid value of {value} has been configured for setting [{key}] under group [{group}] for type of {typeof(T)}",
                    e);
            }
        }
    }
}