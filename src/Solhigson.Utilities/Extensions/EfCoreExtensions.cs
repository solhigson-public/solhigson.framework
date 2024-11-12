using Microsoft.EntityFrameworkCore;

namespace Solhigson.Utilities.Extensions;

public static class EfCoreExtensions
{
    public static bool IsDbSetType(this Type? type)
    {
        if (type is null)
        {
            return false;
        }
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DbSet<>);
    }

}