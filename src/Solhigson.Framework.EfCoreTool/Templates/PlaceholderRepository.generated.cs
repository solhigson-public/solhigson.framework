using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Infrastructure;

namespace [Namespace].[Folder]
{
    //[GeneratedFileComment]
    public partial class [Placeholder]Repository : [ApplicationName][Cached]RepositoryBase<[EntityNameSpace].[Placeholder]
        [CachedEntityModel]>, 
            [Namespace].[Folder].[AbstractionsFolder].I[Placeholder]Repository
    {
        public [Placeholder]Repository([DbContextNamespace].[DbContextName] dbContext) : base(dbContext)
        {
        }

[Properties]
    }
}