using System.Text;

namespace Solhigson.Utilities;

public static class EnumerableExtensions
{
    public static string ToConcatenatedString<T>(this IEnumerable<T> source, Func<T, string> selector,
        string separator)
    {
        var b = new StringBuilder();
        bool needSeparator = false;

        foreach (var item in source)
        {
            if (needSeparator)
                b.Append(separator);

            b.Append(selector(item));
            needSeparator = true;
        }

        return b.ToString();
    }

    public static LinkedList<T> ToLinkedList<T>(this IEnumerable<T> source)
    {
        return new LinkedList<T>(source);
    }

}