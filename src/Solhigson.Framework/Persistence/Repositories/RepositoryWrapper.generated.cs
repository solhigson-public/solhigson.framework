
namespace Solhigson.Framework.Persistence.Repositories
{
    /*
     * Generated by: Solhigson.Framework.efcoretool
     *
     * https://github.com/solhigson-public/solhigson.framework
     * https://www.nuget.org/packages/solhigson.framework.efcoretool
     *
     * This file is ALWAYS overwritten, DO NOT place custom code here
     */
    public partial class RepositoryWrapper : Solhigson.Framework.Persistence.Repositories.Abstractions.IRepositoryWrapper
    {
        public Solhigson.Framework.Persistence.SolhigsonDbContext DbContext { get; }

		private Solhigson.Framework.Persistence.Repositories.Abstractions.IAppSettingRepository _appSettingRepository;
		public Solhigson.Framework.Persistence.Repositories.Abstractions.IAppSettingRepository AppSettingRepository
		{ get { return _appSettingRepository ??= new Solhigson.Framework.Persistence.Repositories.AppSettingRepository(DbContext); } }

		private Solhigson.Framework.Persistence.Repositories.Abstractions.INotificationTemplateRepository _notificationTemplateRepository;
		public Solhigson.Framework.Persistence.Repositories.Abstractions.INotificationTemplateRepository NotificationTemplateRepository
		{ get { return _notificationTemplateRepository ??= new Solhigson.Framework.Persistence.Repositories.NotificationTemplateRepository(DbContext); } }


        public RepositoryWrapper(Solhigson.Framework.Persistence.SolhigsonDbContext dbContext)
        {
            DbContext = dbContext;
        }
        
        public System.Threading.Tasks.Task SaveChangesAsync()
        {
            return DbContext.SaveChangesAsync();
        }
                
        public int SaveChanges()
        {
            return DbContext.SaveChanges();
        }
    }
}