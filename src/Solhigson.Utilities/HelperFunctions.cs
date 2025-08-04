using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Solhigson.Utilities.Pluralization;

namespace Solhigson.Utilities;

public static class HelperFunctions
{
    private static readonly EnglishPluralizationService PluralizationService = new();

    private static readonly int[] MAnDeltas = { 0, 1, 2, 3, 4, -4, -3, -2, -1, 0 };

    private static readonly bool[] MAbChecksumAnswers =
    {
        true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true,
        false, false, false, false, false, false, false,
        false, false, true
    };


    public const string MatchEmailPattern =
        @"\A(?:[a-z0-9A-Z!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9A-Z](?:[a-zA-Z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?)\Z";

    public const string MatchPhoneNumberPattern =
        @"^\\+?[0-9 ]{11,15}$";

    private static readonly Regex EmailMatchRegex = new Regex(MatchEmailPattern,
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PhoneNumberMatchRegex = new Regex(MatchEmailPattern,
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValidEmailAddress(string email, bool ignoreEmpty = false)
    {
        return string.IsNullOrWhiteSpace(email)
            ? ignoreEmpty
            : EmailMatchRegex.IsMatch(email);
    }

    public static bool IsValidPhoneNumber(string phoneNumber, bool ignoreEmpty = false)
    {
        return string.IsNullOrWhiteSpace(phoneNumber)
            ? ignoreEmpty
            : PhoneNumberMatchRegex.IsMatch(phoneNumber);
    }

    public static string Capitalize(string word)
    {
        return PluralizationService.Capitalize(word);
    }

    public static string GetCallerIp(HttpContext httpContext)
    {
        if (httpContext is null)
        {
            return string.Empty;
        }

        var caller = "";
        // if you are allowing these forward headers, please ensure you are restricting context.Connection.RemoteIpAddress
        // to cloud flare ips: https://www.cloudflare.com/ips/
        var header = httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ??
                     httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (header != null)
            if (IPAddress.TryParse(header, out var ip))
                caller = ip.ToString();

        var add = httpContext?.Connection.RemoteIpAddress;
        if (add != null) caller = add.ToString();

        return caller;
    }

    public static string TimespanToWords(TimeSpan timeSpan)
    {
        double value;
        string postfix;
        var determinePlural = true;
        if (timeSpan.TotalMinutes < 1)
        {
            if (timeSpan.TotalSeconds < 1)
            {
                value = 1;
            }
            else
            {
                value = Math.Round(timeSpan.TotalSeconds, 2, MidpointRounding.AwayFromZero);
            }

            postfix = " sec";
        }
        else if (timeSpan.TotalHours < 1)
        {
            value = Math.Round(timeSpan.TotalMinutes, 2, MidpointRounding.AwayFromZero);
            postfix = " min";
        }
        else if (timeSpan.TotalDays < 1)
        {
            value = Math.Round(timeSpan.TotalHours, 2, MidpointRounding.AwayFromZero);
            postfix = " hr";
        }
        else if (timeSpan.TotalDays < 31)
        {
            value = Math.Round(timeSpan.TotalDays, 2, MidpointRounding.AwayFromZero);
            postfix = " day";
        }
        else if (timeSpan.TotalDays < 365)
        {
            var months = Math.Truncate(timeSpan.TotalDays / 31);
            var remainingDays = timeSpan.TotalDays - (months * 31);
            value = months;
            var monthPostfix = months > 1 ? "s" : "";
            var dayPostfix = remainingDays > 1 ? "s" : "";
            postfix = $" month{monthPostfix}";
            if (remainingDays > 0)
            {
                postfix += $" {remainingDays} day{dayPostfix}";
            }

            determinePlural = false;
        }
        else
        {
            var years = Math.Truncate(timeSpan.TotalDays / 365);
            var remainingDays = timeSpan.TotalDays - (years * 365);
            var remainingMonths = 0d;
            if (remainingDays > 31)
            {
                remainingMonths = Math.Truncate(remainingDays / 31);
                remainingDays -= (remainingMonths * 31);
            }

            value = years;
            var yearsPostfix = years > 1 ? "s" : "";
            var monthsPostfix = remainingMonths > 1 ? "s" : "";
            var dayPostfix = remainingDays > 1 ? "s" : "";
            postfix = $" year{yearsPostfix}";
            if (remainingMonths > 0)
            {
                postfix += $" {remainingMonths} month{monthsPostfix}";
            }

            if (remainingDays > 0)
            {
                postfix += $" {remainingDays} day{dayPostfix}";
            }

            determinePlural = false;
        }

        if (determinePlural && value > 1) postfix += "s";
        return value + postfix;
    }

    public static bool IsServiceUp(HttpResponse response, string responseBody = null)
    {
        return response.StatusCode < 500;
    }

    public static bool IsServiceUp(HttpStatusCode statusCode)
    {
        return (int)statusCode < 500;
    }


    public static JObject ToJsonObject(IEnumerable<KeyValuePair<string, string>> dictionary)
    {
        if (dictionary == null)
        {
            return null;
        }

        var obj = new JObject();
        foreach (var (key, value) in dictionary)
        {
            obj.Add(key, value);
        }

        return obj;
    }

    public static JObject? ToJsonObject(HttpResponseHeaders? headers)
    {
        if (headers == null)
        {
            return null;
        }

        var obj = new JObject();
        foreach (var (key, value) in headers)
        {
            obj.Add(key, string.Join(",", value));
        }

        return obj;
    }


    public static JObject? ToJsonObject(IHeaderDictionary? dictionary)
    {
        if (dictionary == null)
        {
            return null;
        }

        var obj = new JObject();
        foreach (var (key, value) in dictionary)
        {
            if (string.Compare(key, "cookie", StringComparison.OrdinalIgnoreCase) == 0)
            {
                continue;
            }

            obj.Add(key, value.ToString());
        }

        return obj;
    }

    public static string? CheckForProtectedFields(string data, IReadOnlyCollection<string>? protectedFields)
    {
        if (!IsValidJson(data) || protectedFields?.Count == 0) //json only
        {
            return data;
        }

        try
        {
            var jObject = JToken.Parse(data);
            var result = CheckForProtectedFields(jObject, protectedFields);
            if (result != null)
            {
                return result.ToString();
            }
        }
        catch (Exception e)
        {
            //Logger.Error(e, "MaskProtectedFields: While trying to parse json data");
        }

        return data;
    }

    public static JObject? CheckForProtectedFields(JObject? jObject, IReadOnlyCollection<string>? protectedFields)
    {
        if (jObject == null || protectedFields?.Count == 0)
        {
            return jObject;
        }

        try
        {
            var fields = jObject.Properties();
            foreach (var prop in fields)
            {
                MaskProtectedProperties(jObject, prop, protectedFields);
            }

            return jObject;
        }
        catch (Exception e)
        {
            //Logger.Error(e, "MaskProtectedFields: While trying to parse json data");
        }

        return null;
    }

    public static object? CheckForProtectedFields(object? data, IReadOnlyCollection<string>? protectedFields)
    {
        if(!protectedFields?.Any() == false)
        {
            return data;
        }
        switch (data)
        {
            case null:
                return null;
            case string:
                return data;
            default:
                try
                {
                    var jObject = JObject.FromObject(data);
                    return CheckForProtectedFields(jObject, protectedFields);
                }
                catch (Exception e)
                {
                    //Logger.Error(e, "CheckForProtectedFields(object data)", data);
                }

                return data;
        }
    }

    private static void MaskProtectedProperties(JObject? jObject, JProperty? jProperty, IReadOnlyCollection<string>? protectedFields)
    {
        if(jObject is null || string.IsNullOrWhiteSpace(jProperty?.Name) || protectedFields.Count == 0)
        {
            return;
        }
        
        if (protectedFields.Contains(jProperty.Name, StringComparer.OrdinalIgnoreCase))
        {
            jObject.Property(jProperty.Name)!.Value = "******";
        }

        if (jProperty.Value is JValue)
        {
            try
            {
                var data = jProperty.Value.ToString();
                if (IsValidJson(data, out var isArray))
                {
                    jProperty.Value = isArray
                        ? JArray.Parse(data)
                        : JObject.Parse(data);
                }
            }
            catch (Exception e)
            {
                //Logger.Error(e, $"Parsing {jProperty.Name} data");
            }
        }

        if (jProperty.Value is JArray array)
        {
            foreach (var jChildObject in array)
            {
                if (jChildObject is JObject obj)
                {
                    CheckForProtectedFields(obj, protectedFields);
                }
            }

            return;
        }

        if (jProperty.Value is not JObject childObject)
        {
            return;
        }

        CheckForProtectedFields(childObject, protectedFields);
    }

    public static bool IsValidJson(string strInput)
    {
        return IsValidJson(strInput, out _);
    }


    public static bool IsValidJson(string strInput, out bool isArray)
    {
        isArray = false;
        if (string.IsNullOrWhiteSpace(strInput))
        {
            return false;
        }

        strInput = strInput.Trim();
        if (strInput.StartsWith("{") && strInput.EndsWith("}"))
        {
            return true;
        }

        if (strInput.StartsWith("[") && strInput.EndsWith("]"))
        {
            isArray = true;
            return true;
        }

        return false;
    }

    public static bool IsValidXml(string strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput))
        {
            return false;
        }

        strInput = strInput.Trim();
        return strInput.StartsWith("<") && strInput.EndsWith(">");
    }

    public static string FormatCurrency(decimal? amount, string symbol = "₦")
    {
        return FormatAmountInternal(amount, symbol);
    }

    private static string FormatAmountInternal(decimal? amount, string symbol = "₦", short decimalDigits = 2)
    {
        symbol ??= "";
        var numberFormatInfo = new NumberFormatInfo
            { CurrencySymbol = symbol, CurrencyDecimalDigits = decimalDigits };
        if (amount == null)
        {
            return 0.ToString("c", numberFormatInfo);
        }

        return (amount.Value).ToString("c", numberFormatInfo);
    }

    public static string FormatAmount(decimal? amount, short decimalDigits = 0)
    {
        return FormatAmountInternal(amount, "", decimalDigits);
    }

    public static string SeparatePascalCaseWords(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 1) Count required length = original chars + number of spaces
        var spaceCount = 0;
        for (var i = 1; i < input.Length; i++)
        {
            char prev = input[i - 1], curr = input[i];
            var boundary =
                (char.IsLower(prev) && char.IsUpper(curr))
                || (char.IsUpper(prev)
                    && char.IsUpper(curr)
                    && i + 1 < input.Length
                    && char.IsLower(input[i + 1]))
                || (char.IsLetter(prev) && !char.IsLetter(curr))
                || (!char.IsLetter(prev) && char.IsLetter(curr));

            if (boundary) spaceCount++;
        }

        var resultLength = input.Length + spaceCount;

        // 2) Build exactly that many chars
        return string.Create(resultLength, input, (span, s) =>
        {
            var dst = 0;
            span[dst++] = char.ToUpper(s[0]);

            for (var i = 1; i < s.Length; i++)
            {
                char prev = s[i - 1], curr = s[i];
                var boundary =
                    (char.IsLower(prev) && char.IsUpper(curr))
                    || (char.IsUpper(prev)
                        && char.IsUpper(curr)
                        && i + 1 < s.Length
                        && char.IsLower(s[i + 1]))
                    || (char.IsLetter(prev) && !char.IsLetter(curr))
                    || (!char.IsLetter(prev) && char.IsLetter(curr));

                if (boundary)
                    span[dst++] = ' ';

                span[dst++] = curr;
            }
        });

        // var result = Regex.Replace(
        //     input,
        //     @"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])",
        //     " ",
        //     RegexOptions.Compiled).Trim();
        // return result[..1].ToUpper() + result[1..];
    }

    public static string ReplacePlaceHolders(string text, IDictionary<string, string>? placeholders)
    {
        if (string.IsNullOrWhiteSpace(text) || placeholders == null || placeholders.Count == 0)
            return text;

        // Enumerate KeyValuePair so we only do one dictionary lookup per entry
        foreach (var kv in placeholders)
        {
            text = text.Replace(kv.Key, kv.Value);
        }

        return text;
    }

    public static T? SafeGetSessionData<T>(string key, HttpContext? httpContext) where T : class
    {
        try
        {
            var obj = httpContext?.Session.GetString(key) ?? Thread.GetData(Thread.GetNamedDataSlot(key)) as string;
            if (obj?.Contains('{') == true)
            {
                return obj.DeserializeFromJson<T>();
            }

            return obj as T;
        }
        catch (Exception e)
        {
            //Logger.Error(e);
            return null;
        }
    }

    public static T? SafeGetSessionData<T>(string key, IHttpContextAccessor? httpContextAccessor) where T : class
    {
        return SafeGetSessionData<T>(key, httpContextAccessor?.HttpContext);
    }

    public static string? SafeGetSessionData(string key, IHttpContextAccessor? httpContextAccessor)
    {
        return SafeGetSessionData<string>(key, httpContextAccessor?.HttpContext);
    }

    public static string? SafeGetSessionData(string key, HttpContext? httpContext)
    {
        return SafeGetSessionData<string>(key, httpContext);
    }

    public static void SafeSetSessionData(string key, object value, IHttpContextAccessor? httpContextAccessor)
    {
        SafeSetSessionData(key, value, httpContextAccessor?.HttpContext);
    }

    public static void SafeSetSessionData(string key, object? value, HttpContext? httpContext)
    {
        try
        {
            if (value is null)
            {
                return;
            }

            var data = value is not string ? value.SerializeToJson()! : value.ToString()!;
            if (httpContext?.Session != null)
            {
                httpContext.Session.SetString(key, data);
            }
            else
            {
                Thread.SetData(Thread.GetNamedDataSlot(key), data);
            }
        }
        catch (Exception e)
        {
            //Logger.Error(e);
        }
    }

    public static void SafeRemoveSessionData(string key, HttpContext? httpContext)
    {
        try
        {
            if (httpContext?.Session is not null)
            {
                httpContext.Session.Remove(key);
            }
            else
            {
                Thread.SetData(Thread.GetNamedDataSlot(key), null);
            }
        }
        catch (Exception e)
        {
            //Logger.Error(e);
        }
    }

    public static void SafeRemoveSessionData(string key, IHttpContextAccessor? httpContextAccessor)
    {
        SafeRemoveSessionData(key, httpContextAccessor?.HttpContext);
    }

    public static bool IsLuhnNumberValid(string number)
    {
        double num;
        if (!double.TryParse(number, out num))
        {
            return false;
        }

        var checksum = 0;
        var doubleDigit = false;

        var chars = number.ToCharArray();
        for (var i = chars.Length - 1; i > -1; i--)
        {
            var j = chars[i] ^ 0x30;

            checksum += j;

            if (doubleDigit)
            {
                checksum += MAnDeltas[j];
            }

            doubleDigit = !doubleDigit;
        }

        return MAbChecksumAnswers[checksum];
    }

    /// <summary>
    ///   It obfuscates card data (ISO 8583 fields 2, 14, 35 and 45)
    /// </summary>
    /// <param name="data"> The card data. </param>
    /// <param name="showFirstSixDigits"></param>
    /// <returns> The obfuscated data. </returns>
    /// <remarks>
    ///   ObfuscateCardData( 4000000000000002 ) = ************0002
    ///   ObfuscateCardData( 0805 ) = ****
    ///   ObfuscateCardData( 4000000000000002=0805123456 ) = ************0002=**********
    ///   ObfuscateCardData( B4000000000000002^JOHN DOE^0805123456 ) = B************0002^JOHN DOE^**********
    /// </remarks>
    public static string ObfuscateCardData(string data, bool showFirstSixDigits = true)
    {
        var b = new StringBuilder(data.Length);

        var i = data.IndexOf('^');
        var j = -1;
        if (i == -1)
        {
            // Try track 2, determine the correct field separator (valids are 'D' o '=').
            i = data.IndexOf('=');
            if (i == -1)
            {
                i = data.IndexOf('D');
            }

            if ((i == -1) && (data.Length > 11))
            {
                i = data.Length;
            }
        }
        else
        {
            // It's track 1
            j = data.IndexOf('^', i + 1);
        }

        for (var k = 0; k < data.Length; k++)
        {
            if (((k <= i) && (k > (i - 5))) ||
                ((k <= j) && (k > i)))
            {
                b.Append(data[k]);
            }
            else
            {
                if (char.IsDigit(data[k]))
                {
                    if (showFirstSixDigits && k <= 5)
                    {
                        b.Append(data[k]);
                    }
                    else
                    {
                        b.Append('*');
                    }
                }
                else
                {
                    b.Append(data[k]);
                }
            }
        }

        return b.ToString();
    }

    public static T Invoke<T>(this Func<T> method)
    {
        if (method is null)
        {
            return default;
        }
        try
        {
            return method();
        }
        catch (Exception e)
        {
            //method.Method.DeclaringType.ELogError(e);
        }

        return default;
    }

    private const char EqualsChar = '=';
    private const char Slash = '/';
    private const byte SlashByte = (byte)Slash;
    private const char Plus = '+';
    private const byte PlusByte = (byte)Plus;
    private const char Hyphen = '-';
    private const char Underscore = '_';
    public static Guid ToGuidFromBase64String(ReadOnlySpan<char> id)
    {
        Span<char> base64Chars = stackalloc char[24];
        for (var i = 0; i < 22; i++)
        {
            base64Chars[i] = id[i] switch
            {
                Hyphen => Slash,
                Underscore => Plus,
                _ => id[i]
            };
        }

        base64Chars[22] = EqualsChar;
        base64Chars[23] = EqualsChar;

        Span<byte> idBytes = stackalloc byte[16];
        Convert.TryFromBase64Chars(base64Chars, idBytes, out _);
        return new Guid(idBytes);
    }

    public static string ToStringFromGuid(Guid guid)
    {
        Span<byte> guidBytes = stackalloc byte[16];
        Span<byte> base64Bytes = stackalloc byte[24];
        MemoryMarshal.TryWrite(guidBytes, ref guid);
        Base64.EncodeToUtf8(guidBytes, base64Bytes, out _, out _);

        Span<char> finalChars = stackalloc char[22];
        for (var i = 0; i < 22; i++)
        {
            finalChars[i] = base64Bytes[i] switch
            {
                SlashByte => Hyphen,
                PlusByte => Underscore,
                _ => (char)base64Bytes[i]
            };
        }
        return new string(finalChars);
    }
}