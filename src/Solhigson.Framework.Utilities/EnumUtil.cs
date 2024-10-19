using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Solhigson.Framework.Utilities;

public static class EnumUtil
{
    /// <summary>
    /// Returns a key value pair list of the enum names and values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<KeyValuePair<string, string>> GetEnumList(this Enum enumType, bool separateCamelCase = true)
    {
        var type = enumType.GetType();

        if (!type.IsEnum) throw new ArgumentException("T must be an enum type");

        var enumValues = Enum.GetValues(type).Cast<int>();

        var enumList = separateCamelCase 
            ? enumValues.Select(value => new KeyValuePair<string, string>(value.ToString(), FromCamelCase(Enum.GetName(type, value)))).ToList() 
            : enumValues.Select(value => new KeyValuePair<string, string>(value.ToString(), Enum.GetName(type, value))).ToList();

        return enumList.OrderBy(kvp => kvp.Value).ToList();
    }
        
    /// <summary>
    /// Returns a key value pair list of the enums names and values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private static IEnumerable<KeyValuePair<string, string>> GetEnumList<T>(bool separateCamelCase = true, bool useNameAsValue = false) where T : struct
    {
        if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enum type");

        var enumValues = Enum.GetValues(typeof(T)).Cast<int>();

        List<KeyValuePair<string, string>> enumList;
        if (useNameAsValue)
        {
            enumList = separateCamelCase 
                ? enumValues.Select(value => new KeyValuePair<string, string>(FromCamelCase(Enum.GetName(typeof(T), value)), Enum.GetName(typeof(T), value))).ToList() 
                : enumValues.Select(value => new KeyValuePair<string, string>(Enum.GetName(typeof(T), value), Enum.GetName(typeof(T), value))).ToList();
        }
        else
        {
            enumList = separateCamelCase 
                ? enumValues.Select(value => new KeyValuePair<string, string>(FromCamelCase(Enum.GetName(typeof(T), value)), value.ToString())).ToList() 
                : enumValues.Select(value => new KeyValuePair<string, string>(Enum.GetName(typeof(T), value), value.ToString())).ToList();
        }

        return enumList.OrderBy(kvp => kvp.Value).ToList();
    }

    public static List<int> GetEnumValues<T>() where T : struct
    {
        if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enum type");

        return Enum.GetValues(typeof(T)).Cast<int>().OrderBy(t => t).ToList();
    }


    public static Dictionary<int, string> GetEnumDictionary<T>() where T : struct
    {
        if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enum type");

        var enumValues = Enum.GetValues(typeof(T)).Cast<int>().OrderBy(t => t);

        return enumValues.ToDictionary(value => value, value => FromCamelCase(Enum.GetName(typeof(T), value)));
    }

    public static List<SelectListItem> GetEnumSelectList<T>(bool separateCamelCase = true, bool useNameAsValue = false) where T : struct
    {
        return GetEnumList<T>(separateCamelCase, useNameAsValue).Select(keypair => new SelectListItem { Text = keypair.Key, Value = keypair.Value }).ToList();
    }

    /// <summary>
    /// Returns an enum from name string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumNameValue"></param>
    /// <returns></returns>
    public static T ParseEnum<T>(string enumNameValue) where T : struct//, IConvertible
    {
        if (!typeof(T).IsEnum) throw new ArgumentException("T must be an enumerated type");
        if (string.IsNullOrEmpty(enumNameValue))
            return default(T);
        try
        {
            return (T)Enum.Parse(typeof(T), enumNameValue, true);
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// checks if the enum contains the provided type. The enum must values must be bitwise enabled i.e power of 2
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="type">Type of Enum</param>
    /// <param name="value">The value to check</param>
    /// <returns>True or False</returns>
    public static bool Has<T>(this System.Enum type, T value)
    {
        try
        {
            if (((int)(object)type | (int)(object)value) == 0) return true;
            return (((int)(object)type & (int)(object)value) == (int)(object)value) && ((int)(object)value != 0);
        }
        catch
        {
            return false;
        }
    }

    //
    /// <summary>
    /// checks if the enum is only the provided type. The enum must values must be bitwise enabled i.e power of 2
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="type">The enum type</param>
    /// <param name="value">the value to check</param>
    /// <returns>True or False</returns>
    public static bool Is<T>(this System.Enum type, T value)
    {
        try
        {
            return (int)(object)type == (int)(object)value;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// appends a value to an enum. The enum must values must be bitwise enabled i.e power of 2
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="type">The enum type</param>
    /// <param name="value">the enum value to append</param>
    /// <returns>The enum containing the appended value</returns>
    public static T Add<T>(this System.Enum type, T value)
    {
        try
        {
            return (T)(object)(((int)(object)type | (int)(object)value));
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                string.Format(
                    "Could not append value from enumerated type '{0}'.",
                    typeof(T).Name
                ), ex);
        }
    }

    /// <summary>
    /// completely removes the value from the enum. The enum must values must be bitwise enabled i.e power of 2
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="type">The enum type</param>
    /// <param name="value">the enum value to append</param>
    /// <returns>The enum without the removed value</returns>
    public static T Remove<T>(this System.Enum type, T value)
    {
        try
        {
            return (T)(object)(((int)(object)type & ~(int)(object)value));
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                string.Format(
                    "Could not remove value from enumerated type '{0}'.",
                    typeof(T).Name
                ), ex);
        }
    }

    public static string FormatEnum(Enum enumValue)
    {
        return FromCamelCase(enumValue.ToString());
    }


    private static string FromCamelCase(string camelCase)
    {
        if (camelCase == null)
            throw new ArgumentException("Enum name can not be null");

        StringBuilder sb = new StringBuilder(camelCase.Length + 10);
        bool first = true;
        char lastChar = '\0';

        foreach (char ch in camelCase)
        {
            if (!first &&
                (char.IsUpper(ch) ||
                 char.IsDigit(ch) && !char.IsDigit(lastChar)))
                sb.Append(' ');

            sb.Append(ch);
            first = false;
            lastChar = ch;
        }

        return sb.ToString();
    }

    public static Dictionary<int, string> ToDictionary(this Enum @enum)
    {
        var type = @enum.GetType();
        return Enum.GetValues(type).Cast<object>().ToDictionary(e => (int)e, e => Enum.GetName(type, e));
    }

    public static Dictionary<int, string> ToDictionary<T>()
        where T : struct
    {
        var template = new T();
        var type = template.GetType();
        return Enum.GetValues(type).Cast<object>().ToDictionary(e => (int)e, e => Enum.GetName(type, e));
    }

    public static T GetEnumItem<T>(int flag)
    {
        var enumItem = (T)Enum.ToObject(typeof(T), flag);
        return enumItem;
    }


}