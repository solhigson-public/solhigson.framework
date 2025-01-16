using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Dto;
using Solhigson.Framework.EfCore;
using Solhigson.Framework.Persistence.EntityModels;
using Solhigson.Framework.Persistence.Repositories.Abstractions;
using Solhigson.Utilities.Extensions;
using Solhigson.Utilities.Security;

namespace Solhigson.Framework.Services;

public class SolhigsonConfigurationService(IRepositoryWrapper repositoryWrapper) : ServiceBase(repositoryWrapper)
{
    private static readonly byte[] EncryptionKey = ("67566B59703373367639792F423F45284" +
                                                    "82B4D6251655468576D5A7134743777").FromHexString();

    private static readonly byte[] EncryptionIv = "73357638792F423F4528482B4D625065".FromHexString();

    private const string EncryptDisplay = "@@@***Encrypted***@@@";

    public async Task<ResponseInfo> CreateApplicationSettingAsync(AppSetting appSetting)
    {
        var existing =
            await RepositoryWrapper.AppSettingRepository.GetByNameAsync(appSetting.Name);
        if (existing is not null)
        {
            return ResponseInfo.FailedResult($"AppSetting with name already exists: {existing.Name}");
        }

        if (MaskForSaveIfSensitive(appSetting))
        {
            RepositoryWrapper.DbContext.Add(appSetting);
            await RepositoryWrapper.SaveChangesAsync();
        }

        return ResponseInfo.SuccessResult();
    }
        
    public async Task<ResponseInfo<AppSetting>> GetApplicationSettingAsync(string name)
    {
        var response = new ResponseInfo<AppSetting>();
        var setting = await RepositoryWrapper.AppSettingRepository.GetByNameAsync(name);
        MaskForDisplayIfSensitive(setting);
        return setting is not null ? response.Success(setting) : response.Fail();
    }

    public async Task<ResponseInfo> UpdateApplicationSettingAsync(AppSetting appSetting)
    {
        if (MaskForSaveIfSensitive(appSetting))
        {
            var setting = await RepositoryWrapper.AppSettingRepository.GetByIdAsync(appSetting.Id);
            setting.Value = appSetting.Value;
            setting.IsSensitive = appSetting.IsSensitive;
            await RepositoryWrapper.SaveChangesAsync();
        }

        return ResponseInfo.SuccessResult();
    }
        
    public async Task<ResponseInfo> DeleteApplicationSettingAsync(AppSetting appSetting)
    {
        RepositoryWrapper.DbContext.Remove(appSetting);
        await RepositoryWrapper.SaveChangesAsync();
        return ResponseInfo.SuccessResult();
    }
        
    public async Task<ResponseInfo<PagedList<AppSetting>>> SearchAppSettingsAsync(int page = 1, int pageSize = 20,
        string? name = null)
    {
        var response = new ResponseInfo<PagedList<AppSetting>>();
        var query = RepositoryWrapper.DbContext.AppSettings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(t => t.Name.Contains(name));
        }

        var result = await query.ToPagedListAsync(page, pageSize);
        foreach (var setting in result.Results)
        {
            MaskForDisplayIfSensitive(setting);
        }
        return response.Success(result);
    }



    public async Task<ResponseInfo> SaveNotificationTemplateAsync(NotificationTemplate notificationTemplate)
    {
        var existing =
            await RepositoryWrapper.NotificationTemplateRepository.GetByNameAsync(notificationTemplate.Name);
        if (existing is not null)
        {
            existing.Template = notificationTemplate.Template;
        }
        else
        {
            RepositoryWrapper.DbContext.Add(notificationTemplate);
        }
        await RepositoryWrapper.SaveChangesAsync();
        return ResponseInfo.SuccessResult();
    }
        
    public async Task<ResponseInfo<NotificationTemplate>> GetNotificationTemplateAsync(string name)
    {
        var response = new ResponseInfo<NotificationTemplate>();
        var template = await RepositoryWrapper.NotificationTemplateRepository.GetByNameAsync(name);
        return template is not null ? response.Success(template) : response.Fail();
    }

    public async Task<ResponseInfo> DeleteNotificationTemplateAsync(NotificationTemplate notificationTemplate)
    {
        RepositoryWrapper.DbContext.Remove(notificationTemplate);
        await RepositoryWrapper.SaveChangesAsync();
        return ResponseInfo.SuccessResult();
    }

    public async Task<ResponseInfo<PagedList<NotificationTemplate>>> SearchNotificationTemplatesAsync(int page = 1, int pageSize = 20,
        string? name = null)
    {
        var response = new ResponseInfo<PagedList<NotificationTemplate>>();
        var query = RepositoryWrapper.DbContext.NotificationTemplates.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(t => t.Name.Contains(name));
        }

        var result = await query.ToPagedListAsync(page, pageSize);
        return response.Success(result);
    }

    private static bool MaskForSaveIfSensitive(AppSetting? setting)
    {
        if (setting is null)
        {
            return false;
        }
        if (!setting.IsSensitive)
        {
            return true;
        }
        if (setting.Value == EncryptDisplay)
        {
            return false;
        }
        setting.Value = EncryptSetting(setting.Value);
        return true;
    }

    private static void MaskForDisplayIfSensitive(AppSetting? setting)
    {
        if (setting is null)
        {
            return;
        }
        if (setting.IsSensitive)
        {
            setting.Value = EncryptDisplay;
        }
    }


    private static string EncryptSetting(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return string.Empty;
        }
        return Convert.ToBase64String(CryptoHelper.SymmetricEncryptAsync(Encoding.UTF8.GetBytes(data),
            EncryptionKey, EncryptionIv, EncryptionModes.Aes, PaddingMode.PKCS7).Result);
    }
        
    public static string DecryptSetting(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return string.Empty;
        }
        return Encoding.UTF8.GetString(CryptoHelper.SymmetricDecryptAsync(Convert.FromBase64String(data),
            EncryptionKey, EncryptionIv, EncryptionModes.Aes, PaddingMode.PKCS7).Result);
    }

}