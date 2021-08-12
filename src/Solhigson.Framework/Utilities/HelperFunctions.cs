﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Logging;

namespace Solhigson.Framework.Utilities
{
    public static class HelperFunctions
    {
        private static readonly LogWrapper Logger = LogManager.GetCurrentClassLogger();

        public static string GetCallerIp(HttpContext httpContext)
        {
            if (httpContext is null)
            {
                return string.Empty;
            }
            var caller = "";
            if (httpContext == null) return caller;
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
                        var njObject = JToken.Parse(data);
                        if (njObject is JArray)
                        {
                            foreach (var obj in njObject)
                            {
                                if (obj is JValue)
                                {
                                    continue;
                                }

                                CheckForProtectedFields((JObject) obj, protectedFields);
                            }
                        }

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
            return FormatAmountInternal(amount, symbol, false);
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
    }
}