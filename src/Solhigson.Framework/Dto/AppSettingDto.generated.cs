using System;

namespace Solhigson.Framework.Dto
{
    /*
     * Generated by: Solhigson.Framework.efcoretool
     *
     * https://github.com/solhigson-public/solhigson.framework
     * https://www.nuget.org/packages/solhigson.framework.efcoretool
     *
     * This file is ALWAYS overwritten, DO NOT place custom code here
     */
    public partial record AppSettingDto
    { 
		public int Id { get; set; }
		public string Name { get; set; }
		public string Value { get; set; }
		public bool IsSensitive { get; set; }

    }
}