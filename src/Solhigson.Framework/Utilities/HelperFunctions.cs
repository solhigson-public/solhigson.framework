using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;
using Solhigson.Framework.Utilities.Pluralization;

namespace Solhigson.Framework.Utilities
{
    public static class HelperFunctions
    {
        private static readonly LogWrapper Logger = new LogWrapper(typeof(HelperFunctions).FullName);
        private static readonly EnglishPluralizationService PluralizationService = new ();
        
        public const string MatchEmailPattern =
            @"\A(?:[a-z0-9A-Z!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9A-Z](?:[a-zA-Z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?)\Z";

        public const string MatchPhoneNumberPattern =
            @"^\\+?[0-9 ]{11,15}$";

        private static readonly Regex EmailMatchRegex = new Regex(MatchEmailPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex PhoneNumberMatchRegex = new Regex(MatchEmailPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

        public static string Format(TimeSpan timeSpan)
        {
            double value;
            string postfix;
            if (timeSpan.TotalMinutes < 1)
            {
                value = Math.Round(timeSpan.TotalSeconds, 2, MidpointRounding.AwayFromZero);
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
            else
            {
                value = Math.Round(timeSpan.TotalDays, 2, MidpointRounding.AwayFromZero);
                postfix = " day";
            }

            if (value > 1) postfix += "s";
            return value + postfix;
        }

        public static bool IsServiceUp(HttpResponse response, string responseBody = null)
        {
            return response.StatusCode < 500;
        }

        public static bool IsServiceUp(HttpStatusCode statusCode)
        {
            return (int) statusCode < 500;
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

        public static JObject ToJsonObject(HttpResponseHeaders headers)
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


        public static JObject ToJsonObject(IHeaderDictionary dictionary)
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

        public static string CheckForProtectedFields(string data, List<string> protectedFields)
        {
            if (!IsValidJson(data)) //json only
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
                Logger.Error(e, "MaskProtectedFields: While trying to parse json data");
            }

            return data;
        }

        public static JObject CheckForProtectedFields(JObject jObject, List<string> protectedFields)
        {
            if (jObject == null)
            {
                return null;
            }

            try
            {
                var fields = jObject.Properties().ToList();
                foreach (var prop in fields)
                {
                    MaskProtectedProperties(jObject, prop, protectedFields);
                }

                return jObject;
            }
            catch (Exception e)
            {
                Logger.Error(e, "MaskProtectedFields: While trying to parse json data");
            }

            return null;
        }

        public static object CheckForProtectedFields(object data, List<string> protectedFields)
        {
            switch (data)
            {
                case null:
                    return null;
                case string _:
                    return data;
                default:
                    try
                    {
                        var jObject = JObject.FromObject(data);
                        return CheckForProtectedFields(jObject, protectedFields);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "CheckForProtectedFields(object data)", data);
                    }

                    return data;
            }
        }

        private static void MaskProtectedProperties(JObject jObject, JProperty jProperty, List<string> protectedFields)
        {
            if (protectedFields.Contains(jProperty.Name, StringComparer.OrdinalIgnoreCase))
            {
                jObject.Property(jProperty.Name).Value = "******";
            }

            if (jProperty.Name == "RequestMessage" || jProperty.Name == "ResponseMessage")
            {
                try
                {
                    var data = jProperty.Value.ToString();
                    if (IsValidJson(data))
                    {
                        var njObject = JObject.Parse(data);
                        CheckForProtectedFields(njObject, protectedFields);
                        /*
                        if (njObject is JArray)
                        {
                            foreach (var obj in njObject)
                            {
                                if (obj is JValue)
                                {
                                    continue;
                                }

                                CheckForProtectedFields(JObject.Parse(data), protectedFields);
                            }
                        }
                        */

                        jProperty.Value = njObject;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Parsing {jProperty.Name} data");
                }
            }

            if (!(jProperty.Value is JObject childObject))
            {
                return;
            }

            var childProperties = childObject.Properties();
            foreach (var cProp in childProperties)
            {
                MaskProtectedProperties(childObject, cProp, protectedFields);
            }
        }

        public static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput))
            {
                return false;
            }

            strInput = strInput.Trim();
            return strInput.StartsWith("{") && strInput.EndsWith("}") || //For object
                   strInput.StartsWith("[") && strInput.EndsWith("]");
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
            return FormatAmountInternal(amount, symbol + " ", false);
        }

        private static string FormatAmountInternal(decimal? amount, string symbol = "₦", bool divideBy100 = true,
            short decimalDigits = 2)
        {
            var numberFormatInfo = new NumberFormatInfo
                {CurrencySymbol = symbol, CurrencyDecimalDigits = decimalDigits};
            if (amount == null)
            {
                return 0.ToString("c", numberFormatInfo);
            }

            if (divideBy100)
            {
                amount = amount / 100m;
            }

            //if (amt == 0)
            //    return "0";
            return (amount.Value).ToString("c", numberFormatInfo);
        }


        public static string FormatAmount(decimal? amount, string symbol = "₦", bool divideBy100 = true,
            short decimalDigits = 2)
        {
            return FormatAmountInternal(amount, symbol + " ", divideBy100, decimalDigits);
        }

        public static string SeparatePascalCaseWords(string input)
        {
            //"((?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z]))"

            var result = Regex.Replace(
                input,
                @"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])",
                " ",
                RegexOptions.Compiled).Trim();
            return result[..1].ToUpper() + result[1..];
        }
        
        public static string ReplacePlaceHolders(string text, IDictionary<string, string> placeHolders)
        {
            if (!string.IsNullOrWhiteSpace(text) && placeHolders?.Count > 0)
            {
                text = placeHolders.Keys.Aggregate(text,
                    (current, placeHolder) =>
                        current.Replace(placeHolder, placeHolders[placeHolder]));
            }
            return text;
        }
        
        public static T SafeGetSessionData<T>(string key, HttpContext httpContext) where T : class
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
                Logger.Error(e);
                return null;
            }
        }

        public static T SafeGetSessionData<T>(string key, IHttpContextAccessor httpContextAccessor) where T : class
        {
            return SafeGetSessionData<T>(key, httpContextAccessor?.HttpContext);
        }
        
        public static string SafeGetSessionData(string key, IHttpContextAccessor httpContextAccessor)
        {
            return SafeGetSessionData<string>(key, httpContextAccessor?.HttpContext);
        }
        
        public static string SafeGetSessionData(string key, HttpContext httpContext)
        {
            return SafeGetSessionData<string>(key, httpContext);
        }

        public static void SafeSetSessionData(string key, object value, IHttpContextAccessor httpContextAccessor)
        {
            SafeSetSessionData(key, value, httpContextAccessor?.HttpContext);
        }
        public static void SafeSetSessionData(string key, object value, HttpContext httpContext)
        {
            try
            {
                if (value is null)
                {
                    return;
                }
                var data = value is not string ? value.SerializeToJson() : value.ToString();
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
                Logger.Error(e);
            }
        }

        public static void SafeRemoveSessionData(string key, HttpContext httpContext)
        {
            try
            {
                if (httpContext?.Session != null)
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
                Logger.Error(e);
            }
        }

        public static void SafeRemoveSessionData(string key, IHttpContextAccessor httpContextAccessor)
        {
            SafeRemoveSessionData(key, httpContextAccessor?.HttpContext);
        }


    }
}