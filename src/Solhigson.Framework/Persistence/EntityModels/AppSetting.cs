﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Solhigson.Framework.Data;
using Solhigson.Framework.Data.Caching;

namespace Solhigson.Framework.Persistence.EntityModels
{
    [Table(ScriptsManager.AppSettingInfo.TableName)]
    public record AppSetting : ICachedEntity
    {
        [Key]
        [Column(ScriptsManager.AppSettingInfo.IdColumn)]
        public int Id { get; set; }
        
        [StringLength(255)]
        [Column(ScriptsManager.AppSettingInfo.NameColumn, TypeName = "VARCHAR")]
        public string Name { get; set; }
        
        [Column(ScriptsManager.AppSettingInfo.ValueColumn, TypeName = "VARCHAR")]
        public string Value { get; set; }
    }
}