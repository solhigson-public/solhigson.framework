using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels
{
    [Table("__SolhigsonPermissions")]
    public record Permission : ICachedEntity
    {
        [Key]
        [StringLength(450)]
        [Column("Id", TypeName = "VARCHAR")]
        public string Id { get; set; }

        [StringLength(255)]
        [Column("Name", TypeName = "VARCHAR")]
        public string Name { get; set; }
    }
}