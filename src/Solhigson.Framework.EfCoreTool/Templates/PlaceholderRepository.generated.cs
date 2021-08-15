using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Infrastructure;

namespace [PersistenceProjectRootNamespace].[Folder]
{
    //[GeneratedFileComment]
    public partial class [Placeholder]Repository : [ApplicationName][Cached]RepositoryBase<[EntityNameSpace].[Placeholder]
        [CachedEntityModel]>, 
            [PersistenceProjectRootNamespace].[Folder].[AbstractionsFolder].I[Placeholder]Repository
    {
        public [Placeholder]Repository([DbContextNamespace].[DbContextName] dbContext) : base(dbContext)
        {
        }

[Properties]
    }
}