using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Persistence;
using Solhigson.Framework.Persistence.EntityModels;
using Solhigson.Framework.Services;
using Solhigson.Framework.Utilities.Security;

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
            /*
            var opt = new DbContextOptionsBuilder<SolhigsonDbContext>();
            //_dbContext = serviceProvider.GetService<SolhigsonDbContext>();
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                opt.UseSqlServer(connectionString);
                _dbContext = new SolhigsonDbContext(opt.Options);
            }
        */
        }

        public T GetFromAppSettingFileOnly<T>(string group, string key = null, string defaultValue = null)
        {
            var setting = GetConfigInternal(group, key, defaultValue, true);
            return VerifySetting<T>(setting, key, group, defaultValue);
        }
        
        public T GetConfig<T>(string groupName, string key = null, string defaultValue = null,
            bool useAppSettingsFileOnly = false)
        {
            var setting = GetConfigInternal(groupName, key, defaultValue);
            return VerifySetting<T>(setting, key, groupName, defaultValue);
        }

        private string GetConfigInternal(string group, string key = null, string defaultValue = null,
            bool useAppSettingsFileOnly = false)
        {
            var configKey = group;
            if (!string.IsNullOrWhiteSpace(key))
            {
                configKey += $":{key}";
            }
            
            var value = Configuration[configKey];
            // ReSharper disable once InconsistentlySynchronizedField
            if (useAppSettingsFileOnly || _dbContext is null)
            {
                return value ??
                       throw new Exception(
                           $"Configuration [{key}] for group [{group}] not found in appSettings file.");
            }

            // ReSharper disable once InconsistentlySynchronizedField
            var query = _dbContext.AppSettings.Where(t => t.Name == configKey)
                .Select(t => t);

            var cacheValue = query.GetCustomResultFromCache<string>();
            if (cacheValue is not null)
            {
                return cacheValue;
            }

            if (value is not null) //give preference to value from appSettings
            {
                query.AddCustomResultToCache(value);
                AddSettingToDb(configKey, value);
                return value;
            }
            
            var appSetting = query.FirstOrDefault();
            if (appSetting is not null)
            {
                value = appSetting.IsSensitive
                    ? SolhigsonConfigurationService.DecryptSetting(appSetting.Value)
                    : appSetting.Value;
                query.AddCustomResultToCache(value);
                return value;
            }

            if (defaultValue is null)
            {
                throw new Exception($"Configuration [{key}] for group [{@group}] not found.");
            }

            AddSettingToDb(configKey, defaultValue);
            return defaultValue;

        }

        private void AddSettingToDb(string key, string value)
        {
            try
            {
                /*
                lock (SyncHelper)
                {
                    */
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
                    //CacheManager.AddToCache(query.GetCacheKey(), value, new List<Type> {typeof(AppSetting)});
                /*
                }
            */
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