using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Identity
{
    [Table(ScriptsManager.RolePermissionInfo.TableName)]
    [Index(nameof(RoleId), nameof(PermissionId), IsUnique = true)]
    [Index(nameof(RoleId))]
    public record SolhigsonRolePermission<T> : ICachedEntity where T : IEquatable<T>
    {
        [Key]
        [Column(ScriptsManager.RolePermissionInfo.IdColumn)]
        public int Id { get; set; }
        
        [StringLength(450)]
        [Column(ScriptsManager.RolePermissionInfo.RoleIdColumn, TypeName = "VARCHAR")]
        public T RoleId { get; set; }
        
        [StringLength(450)]
        [Column(ScriptsManager.RolePermissionInfo.PermissionIdColumn, TypeName = "VARCHAR")]
        public string PermissionId { get; set; }
        
        [ForeignKey(nameof(PermissionId))]
        public SolhigsonPermission SolhigsonPermission { get; set; }

        [ForeignKey(nameof(RoleId))]
        public SolhigsonAspNetRole<T> Role { get; set; }
    }
}