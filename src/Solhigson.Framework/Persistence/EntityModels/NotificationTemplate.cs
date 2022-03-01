using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels;

[Table(ScriptsManager.NotificationTemplateInfo.TableName)]
public record NotificationTemplate : ICachedEntity
{
    [Key]
    [StringLength(255)]
    [Column(ScriptsManager.NotificationTemplateInfo.NameColumn)]
    public string Name { get; set; }
        
    [Column(ScriptsManager.NotificationTemplateInfo.TemplateColumn)]
    [CachedProperty]
    public string Template { get; set; }
}