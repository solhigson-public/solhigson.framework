using System;

namespace Solhigson.Framework.Web.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PermissionAttribute : Attribute
    {
        public PermissionAttribute(string key, string description)
        {
            
        }

        public string Key { get; set; }
        public string Description { get; set; }
    }
}