﻿using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Dto;
using Solhigson.Framework.Extensions;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Persistence.EntityModels;
using Solhigson.Framework.Persistence.Repositories.Abstractions;

namespace Solhigson.Framework.Services
{
    public class SolhigsonConfigurationService : ServiceBase
    {
        public SolhigsonConfigurationService(IRepositoryWrapper repositoryWrapper) : base(repositoryWrapper)
        {
        }
        
        public async Task<ResponseInfo> CreateApplicationSettingAsync(AppSetting appSetting)
        {
            var existing =
                await RepositoryWrapper.AppSettingRepository.GetByNameAsync(appSetting.Name);
            if (existing != null)
            {
                return ResponseInfo.FailedResult($"AppSetting with name already exists: {existing.Name}");
            }
            RepositoryWrapper.DbContext.Add(appSetting);
            await RepositoryWrapper.SaveChangesAsync();
            return ResponseInfo.SuccessResult();
        }
        
        public async Task<ResponseInfo> UpdateApplicationSettingAsync(AppSetting appSetting)
        {
            RepositoryWrapper.DbContext.Update(appSetting);
            await RepositoryWrapper.SaveChangesAsync();
            return ResponseInfo.SuccessResult();
        }
        
        public async Task<ResponseInfo> DeleteApplicationSettingAsync(AppSetting appSetting)
        {
            RepositoryWrapper.DbContext.Remove(appSetting);
            await RepositoryWrapper.SaveChangesAsync();
            return ResponseInfo.SuccessResult();
        }

        public async Task<ResponseInfo> CreateNotificationTemplateAsync(NotificationTemplate notificationTemplate)
        {
            var existing =
                await RepositoryWrapper.NotificationTemplateRepository.GetByNameAsync(notificationTemplate.Name);
            if (existing != null)
            {
                return ResponseInfo.FailedResult($"Template with name already exists: {existing.Name}");
            }
            RepositoryWrapper.DbContext.Add(notificationTemplate);
            await RepositoryWrapper.SaveChangesAsync();
            return ResponseInfo.SuccessResult();
        }
        
        public async Task<ResponseInfo> UpdateNotificationTemplateAsync(NotificationTemplate notificationTemplate)
        {
            RepositoryWrapper.DbContext.Update(notificationTemplate);
            await RepositoryWrapper.SaveChangesAsync();
            return ResponseInfo.SuccessResult();
        }
        
        public async Task<ResponseInfo> DeleteNotificationTemplateAsync(NotificationTemplate notificationTemplate)
        {
            RepositoryWrapper.DbContext.Remove(notificationTemplate);
            await RepositoryWrapper.SaveChangesAsync();
            return ResponseInfo.SuccessResult();
        }

        public async Task<ResponseInfo<PagedList<AppSetting>>> SearchAppSettingsAsync(int page = 1, int pageSize = 20,
            string name = null)
        {
            var response = new ResponseInfo<PagedList<AppSetting>>();
            var query = RepositoryWrapper.DbContext.AppSettings.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(t => t.Name.Contains(name));
            }

            var result = await query.ToPagedListAsync(page, pageSize);
            return response.Success(result);
        }

        public async Task<ResponseInfo<PagedList<NotificationTemplate>>> SearchNotificationTemplatesAsync(int page = 1, int pageSize = 20,
            string name = null)
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

    }
}