using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Identity
{
    [Table(ScriptsManager.RolePermissionInfo.TableName)]
    public record SolhigsonRolePermission<T> : ICachedEntity where T : IEquatable<T>
    {
        [Key]
        public T RoleId { get; set; }
        
        [Key]
        [StringLength(450)]
        [Column(ScriptsManager.RolePermissionInfo.PermissionIdColumn, TypeName = "VARCHAR")]
        public string PermissionId { get; set; }
        
        [ForeignKey(nameof(PermissionId))]
        public SolhigsonPermission SolhigsonPermission { get; set; }

        [ForeignKey(nameof(RoleId))]
        public SolhigsonAspNetRole<T> Role { get; set; }
    }
}