using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels
{
    [Table(CacheManager.AppSettingsTableName)]
    public record AppSetting : ICachedEntity
    {
        [Key]
        [Column(CacheManager.AppSettingsTableIdColumn)]
        public int Id { get; set; }
        
        [StringLength(255)]
        [Column(CacheManager.AppSettingsTableNameColumn, TypeName = "VARCHAR")]
        public string Name { get; set; }
        
        [Column(CacheManager.AppSettingsTableValueColumn, TypeName = "VARCHAR")]
        public string Value { get; set; }
    }
}