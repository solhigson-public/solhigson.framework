using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels
{
    [Table(ScriptsManager.NotificationTemplateInfo.TableName)]
    public record NotificationTemplate : ICachedEntity
    {
        [Key]
        [Column(ScriptsManager.NotificationTemplateInfo.IdColumn)]
        public int Id { get; set; }
        
        [StringLength(255)]
        [Column(ScriptsManager.NotificationTemplateInfo.NameColumn)]
        public string Name { get; set; }
        
        [Column(ScriptsManager.NotificationTemplateInfo.TemplateColumn)]
        public string Template { get; set; }
    }
}