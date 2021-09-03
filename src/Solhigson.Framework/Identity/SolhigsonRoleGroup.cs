using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data.Attributes;

namespace Solhigson.Framework.Identity
{
    [Table("__SolhigsonRoleGroups")]
    [Index(nameof(Name), IsUnique = true)]
    public record SolhigsonRoleGroup
    {
        public SolhigsonRoleGroup()
        {
            Id = Guid.NewGuid().ToString();
        }

        [Key]
        [StringLength(450)]
        [CachedProperty]
        public string Id { get; set; }

        [StringLength(256)]
        [Column("Name")]
        [CachedProperty]
        public string Name { get; set; }
    }
}