using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Identity
{
    [Table(ScriptsManager.RolePermissionInfo.TableName)]
    public record SolhigsonRolePermission<T> : ICachedEntity where T : IEquatable<T>
    {
        public T RoleId { get; set; }
        
        [StringLength(450)]
        [Column(ScriptsManager.RolePermissionInfo.PermissionIdColumn, TypeName = "VARCHAR")]
        public string PermissionId { get; set; }
        
    }
}