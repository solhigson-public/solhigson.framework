using System.Linq;
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
        
        public async Task<ResponseInfo<AppSetting>> GetApplicationSettingAsync(string name)
        {
            var response = new ResponseInfo<AppSetting>();
            var setting = await RepositoryWrapper.AppSettingRepository.GetByNameAsync(name);
            return setting is not null ? response.Success(setting) : response.Fail();
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