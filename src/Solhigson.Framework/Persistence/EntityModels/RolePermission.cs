using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels
{
    [Table("__SolhigsonPermissions")]
    [Index(nameof(RoleId), nameof(PermissionId))]
    public record RolePermission : ICachedEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        
        [StringLength(450)]
        [Column("RoleId", TypeName = "VARCHAR")]
        public string RoleId { get; set; }
        
        [StringLength(255)]
        [Column("PermissionId", TypeName = "VARCHAR")]
        public string PermissionId { get; set; }
    }
}