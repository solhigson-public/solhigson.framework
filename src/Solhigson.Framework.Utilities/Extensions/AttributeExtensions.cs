using System.Reflection;

namespace Solhigson.Framework.Utilities.Extensions;

public static class AttributeExtensions
{
    #region Attributes 
        
    public static T? GetAttribute<T>(this Type? type, bool includeBaseTypes = true) where T : Attribute
    {
        return type?.GetCustomAttributes<T>(includeBaseTypes).FirstOrDefault();
    }
        
    public static T? GetAttribute<T>(this ParameterInfo parameterInfo, bool includeBaseTypes = true) where T : Attribute
    {
        return parameterInfo?.GetCustomAttributes<T>(includeBaseTypes).FirstOrDefault();
    }

    public static T? GetAttribute<T>(this MethodInfo? methodInfo, bool includeBaseTypes = true) where T : Attribute
    {
        return methodInfo?.GetCustomAttributes<T>(includeBaseTypes).FirstOrDefault();
    }

    public static T? GetAttribute<T>(this PropertyInfo? propertyInfo, bool includeBaseTypes = true) where T : Attribute
    {
        return propertyInfo?.GetCustomAttributes<T>(includeBaseTypes).FirstOrDefault();
    }


    public static bool HasAttribute<T>(this Type type, bool includeInheritance = true) where T : Attribute
    {
        return type.GetAttribute<T>(includeInheritance) != null;
    }
        
    public static bool HasAttribute<T>(this ParameterInfo type, bool includeInheritance = true) where T : Attribute
    {
        return type.GetAttribute<T>(includeInheritance) != null;
    }
        
    public static bool HasAttribute<T>(this MethodInfo type, bool includeInheritance = true) where T : Attribute
    {
        return type.GetAttribute<T>(includeInheritance) != null;
    }

    public static bool HasAttribute<T>(this PropertyInfo type, bool includeInheritance = true) where T : Attribute
    {
        return type.GetAttribute<T>(includeInheritance) != null;
    }
        
    #endregion

}