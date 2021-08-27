using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels
{
    [Table(ScriptsManager.PermissionInfo.TableName)]
    public record Permission : ICachedEntity
    {
        [Key]
        [StringLength(450)]
        [Column(ScriptsManager.PermissionInfo.IdColumn, TypeName = "VARCHAR")]
        public string Id { get; set; }

        [StringLength(256)]
        [Column(ScriptsManager.PermissionInfo.NameColumn, TypeName = "VARCHAR")]
        public string Name { get; set; }
    }
}