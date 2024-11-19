using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Persistence.EntityModels;

namespace Solhigson.Framework.Persistence;

public interface ISolhigsonDbContext
{
    DbSet<AppSetting> AppSettings { get; set; }
    DbSet<NotificationTemplate> NotificationTemplates { get; set; }
}