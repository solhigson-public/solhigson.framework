using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Identity;

[Table(ScriptsManager.RoleGroupInfo.TableName)]
[Index(nameof(Name), IsUnique = true)]
public record SolhigsonRoleGroup : ICachedEntity
{
    public SolhigsonRoleGroup()
    {
        Id = NewId.NextSequentialGuid().ToString();
    }

    [Key]
    [StringLength(450)]
    [CachedProperty]
    [Column(ScriptsManager.RoleGroupInfo.IdColumn)]
    public string Id { get; set; }

    [StringLength(256)]
    [Column(ScriptsManager.RoleGroupInfo.NameColumn)]
    [CachedProperty]
    [Required]
    public string Name { get; set; }
}