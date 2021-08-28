using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels
{
    [Table(ScriptsManager.PermissionInfo.TableName)]
    [Index(nameof(Name), IsUnique = true)]
    public record Permission : ICachedEntity
    {
        [Key]
        [StringLength(450)]
        [Column(ScriptsManager.PermissionInfo.IdColumn, TypeName = "VARCHAR")]
        public string Id { get; set; }

        [StringLength(256)]
        [Column(ScriptsManager.PermissionInfo.NameColumn, TypeName = "VARCHAR")]
        [Required]
        public string Name { get; set; }
        
        [StringLength(256)]
        [Column(ScriptsManager.PermissionInfo.DescriptionColumn, TypeName = "VARCHAR")]
        public string Description { get; set; }

        [StringLength(256)]
        [Column(ScriptsManager.PermissionInfo.UrlColumn, TypeName = "VARCHAR")]
        public string Url { get; set; }
        
        [Column(ScriptsManager.PermissionInfo.IsMenuColumn)]
        [Required]
        public bool IsMenu { get; set; }

        [Column(ScriptsManager.PermissionInfo.IsMenuRootColumn)]
        [Required]
        public bool IsMenuRoot { get; set; }
        
        [StringLength(450)]
        [Column(ScriptsManager.PermissionInfo.ParentIdColumn, TypeName = "VARCHAR")]
        public string ParentId { get; set; }

        [Column(ScriptsManager.PermissionInfo.MenuIndexColumn)]
        [Required]
        public int MenuIndex { get; set; }

        [StringLength(256)]
        [Column(ScriptsManager.PermissionInfo.IconColumn, TypeName = "VARCHAR")]
        public string Icon { get; set; }

        [StringLength(256)]
        [Column(ScriptsManager.PermissionInfo.OnClickFunctionColumn, TypeName = "VARCHAR")]
        public string OnClickFunction { get; set; }
        
        [Column(ScriptsManager.PermissionInfo.EnabledColumn)]
        [Required]
        public bool Enabled { get; set; }
    }
}