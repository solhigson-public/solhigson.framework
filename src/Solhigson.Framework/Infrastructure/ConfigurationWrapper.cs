using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Data.Entities;
using Solhigson.Framework.Extensions;

namespace Solhigson.Framework.Infrastructure
{
    public class ConfigurationWrapper
    {
        public IConfiguration Configuration { get; }
        private readonly SolhigsonDbContext _dbContext;
        private static readonly object SyncHelper = new();

        public ConfigurationWrapper(IConfiguration configuration, SolhigsonDbContext dbContext)
        {
            Configuration = configuration;
            _dbContext = dbContext;
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
        
        public string GetConfig(string groupName, string key = null, object defaultValue = null)
        {
            string val = null;
            if (defaultValue != null) val = defaultValue.ToString();
            return GetConfig(groupName, key, val);
        }

        private string GetConfig(string group, string key = null, string defaultValue = null,
            bool useAppSettingsFileOnly = false)
        {
            var configKey = group;
            if (!string.IsNullOrWhiteSpace(key)) configKey += $":{key}";
            var value = Configuration[configKey];

            if (value != null) return value;

            if (!useAppSettingsFileOnly)
            {
                var query = _dbContext.AppSettings.Where(t => t.Name == configKey)
                    .Select(t => t.Value);
                
                value = query.FromCacheSingle();
                if (value != null) return value;
                if (!string.IsNullOrWhiteSpace(defaultValue))
                {
                    AddSettingToDb(query, configKey, defaultValue);
                }
            }

            if (string.IsNullOrWhiteSpace(defaultValue))
            {
                throw new Exception($"Configuration [{key}] for group [{group}] not found.");
            }
            
            return defaultValue;
        }

        private void AddSettingToDb(IQueryable query, string key, string value)
        {
            try
            {
                lock (SyncHelper)
                {
                    if (_dbContext.Set<AppSetting>().Any(t => t.Name == key))
                    {
                        return;
                    }

                    var setting = new AppSetting
                    {
                        Name = key,
                        Value = value
                    };
                    _dbContext.AppSettings.Add(setting);
                    _dbContext.SaveChanges();
                    CacheManager.AddToCache(query.GetCacheKey(), value, typeof(AppSetting));
                }
            }
            catch (Exception e)
            {
                this.ELogError(e, "ConfigWrapper, saving to AppSettings", new { Value = key });
            }
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