using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels
{
    [Table("AspNetRoles")]
    [Index(nameof(Name), IsUnique = true)]
    public record AspNetRole : ICachedEntity
    {
        [Key]
        [StringLength(450)]
        [CachedProperty]
        public string Id { get; set; }
        
        [StringLength(256)]
        [Column("Name")]
        [CachedProperty]
        public string Name { get; set; }
        
        [StringLength(256)]
        [Column("NormalizedName")]
        public string NormalizedName { get; set; }

    }
}