using System;

namespace Solhigson.Framework.Web.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PermissionAttribute : Attribute
    {
        public PermissionAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Description { get; set; }
    }
}