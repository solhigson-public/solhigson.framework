﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Attributes;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Identity;

[Table(ScriptsManager.PermissionInfo.TableName)]
[Index(nameof(Name), IsUnique = true)]
public record SolhigsonPermission : ICachedEntity
{
    public SolhigsonPermission()
    {
        Id = NewId.NextSequentialGuid().ToString();
    }
        
    [Key]
    [StringLength(450)]
    [Column(ScriptsManager.PermissionInfo.IdColumn, TypeName = "VARCHAR")]
    [CachedProperty]
    public string Id { get; set; }

    [StringLength(256)]
    [Column(ScriptsManager.PermissionInfo.NameColumn, TypeName = "VARCHAR")]
    [Required]
    public string Name { get; set; }
        
    [StringLength(256)]
    [Column(ScriptsManager.PermissionInfo.DescriptionColumn, TypeName = "VARCHAR")]
    public string? Description { get; set; }

    [StringLength(256)]
    [Column(ScriptsManager.PermissionInfo.UrlColumn, TypeName = "VARCHAR")]
    public string? Url { get; set; }
        
    [Column(ScriptsManager.PermissionInfo.IsMenuColumn)]
    [Required]
    public bool IsMenu { get; set; }

    [Column(ScriptsManager.PermissionInfo.IsMenuRootColumn)]
    [Required]
    public bool IsMenuRoot { get; set; }
        
    [StringLength(450)]
    [Column(ScriptsManager.PermissionInfo.ParentIdColumn, TypeName = "VARCHAR")]
    public string? ParentId { get; set; }

    [Column(ScriptsManager.PermissionInfo.MenuIndexColumn)]
    [Required]
    public int MenuIndex { get; set; }

    [StringLength(256)]
    [Column(ScriptsManager.PermissionInfo.IconColumn, TypeName = "VARCHAR")]
    public string? Icon { get; set; }

    [StringLength(256)]
    [Column(ScriptsManager.PermissionInfo.OnClickFunctionColumn, TypeName = "VARCHAR")]
    public string? OnClickFunction { get; set; }
        
    [Column(ScriptsManager.PermissionInfo.EnabledColumn)]
    [Required]
    public bool Enabled { get; set; }
        
    [ForeignKey(nameof(ParentId))]
    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public SolhigsonPermission Parent { get; set; }
        
    [InverseProperty(nameof(Parent))]
    public virtual ICollection<SolhigsonPermission>? Children { get; set; }

}