using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solhigson.Framework.Data.Caching;
using Solhigson.Framework.EfCore;
using Solhigson.Framework.EfCore.Caching;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Persistence;
using Solhigson.Framework.Persistence.EntityModels;
using Solhigson.Framework.Services;
using Solhigson.Utilities.Security;

namespace Solhigson.Framework.Infrastructure;

public class ConfigurationWrapper
{
    public IConfiguration Configuration { get; }
    private readonly SolhigsonDbContext _dbContext;
    private static readonly object SyncHelper = new();

    public ConfigurationWrapper(IConfiguration configuration, DbContextOptionsBuilder<SolhigsonDbContext>? optionsBuilder)
    {
        Configuration = configuration;
        if (optionsBuilder is not null)
        {
            _dbContext = new SolhigsonDbContext(optionsBuilder.Options);
        }
    }

    public async ValueTask<T> GetFromAppSettingFileOnlyAsync<T>(string group, string? key = null, string? defaultValue = null)
    {
        var setting = await GetConfigInternalAsync(group, key, defaultValue, true);
        return VerifySetting<T>(setting, key, group, defaultValue);
    }
        
    public async Task<T> GetConfigAsync<T>(string groupName, string? key = null, string? defaultValue = null,
        bool useAppSettingsFileOnly = false)
    {
        var setting = await GetConfigInternalAsync(groupName, key, defaultValue);
        return VerifySetting<T>(setting, key, groupName, defaultValue);
    }

    private async ValueTask<string?> GetConfigInternalAsync(string group, string? key = null, string? defaultValue = null,
        bool useAppSettingsFileOnly = false)
    {
        if (string.IsNullOrWhiteSpace(group) && string.IsNullOrWhiteSpace(key))
        {
            this.LogWarning("GetConfig: group and key are both empty, nothing will be retrieved, returning default value: " + defaultValue);
            return defaultValue;
        }
        var configKey = group;
        if (!string.IsNullOrWhiteSpace(key))
        {
            configKey += $":{key}";
        }
            
        var value = Configuration[configKey];
        if (value is not null) //give preference to value from appSettings File
        {
            return value;
        }

        // ReSharper disable once InconsistentlySynchronizedField
        if (useAppSettingsFileOnly || _dbContext is null)
        {
            if (defaultValue is not null)
            {
                return defaultValue;
            }
            throw new Exception(
                $"Configuration [{configKey}] not found in appSettings.");
        }

        // ReSharper disable once InconsistentlySynchronizedField
        var query = _dbContext.AppSettings.Where(t => t.Name == configKey);//

        //var cacheValue = query.GetCustomResultFromCache<string, AppSetting>();
        var cacheValue = await GetFromCacheAsync(configKey);
        if (cacheValue is not null)
        {
            return cacheValue;
        }
            
        this.LogDebug("Fetching AppSetting [{configKey}] from db", configKey);
        var appSetting = query.FirstOrDefault();
        if (appSetting is not null)
        {
            value = appSetting.IsSensitive
                ? SolhigsonConfigurationService.DecryptSetting(appSetting.Value)
                : appSetting.Value;
            // if (!query.AddCustomResultToCacheAsync(value))
            if (!await AddToCacheAsync(configKey, value))
            {
                this.LogWarning("Adding AppSetting [" + configKey + "] to memory cache was UNSUCCESSFUL");
                //this.LogWarning("Adding AppSetting [{configKey}] to memory cache was UNSUCCESSFUL", configKey);
            }
            return value;
        }

        if (defaultValue is null)
        {
            throw new Exception($"Configuration [{configKey}] not found in appSettings or database.");
        }

        AddSettingToDb(configKey, defaultValue);
        return defaultValue;

    }

    private static async Task<string?> GetFromCacheAsync(string key)
    {
        var result = await EfCoreCacheManager.GetDataAsync<string>(key);
        return result.Data;
    }
    
    private static async Task<bool> AddToCacheAsync(string key, string value)
    {
        return (await EfCoreCacheManager.SetDataAsync(key, value, [typeof(AppSetting)])).IsSuccessful;
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
            //this.ELogWarn($"Adding default AppSetting [{key} - {value}] to database");
            //CacheManager.AddToCacheAsync(query.GetCacheKey(), value, new List<Type> {typeof(AppSetting)});
            /*
            }
        */
        }
        catch (Exception e)
        {
            this.LogError(e, "ConfigWrapper, saving {key} to AppSettings", key);
        }
    }

    private T VerifySetting<T>(string setting, string key, string groupName, object? defaultValue = null)
    {
        try
        {
            return ChangeType<T>(key, setting, groupName);
        }
        catch (Exception e)
        {
            if (defaultValue == null) throw;
            this.LogError(e,
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