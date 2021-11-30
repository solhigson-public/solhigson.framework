using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels
{
    [Table(ScriptsManager.AppSettingInfo.TableName)]
    [Index(nameof(Name), IsUnique = true)]
    public record AppSetting : ICachedEntity
    {
        [Key]
        [Column(ScriptsManager.AppSettingInfo.IdColumn)]
        public int Id { get; set; }
        
        [StringLength(255)]
        [Column(ScriptsManager.AppSettingInfo.NameColumn)]
        public string Name { get; set; }
        
        [Column(ScriptsManager.AppSettingInfo.ValueColumn)]
        public string Value { get; set; }
        
        [Column(ScriptsManager.AppSettingInfo.IsSensitive)]
        public bool IsSensitive { get; set; }

    }
}