using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.Serialization;

namespace Ants
{
    /// <summary>
    /// Extension methods used the Ants.
    /// </summary>
    public static class Extensions
    {
        //source: https://github.com/dotnet/corefx/blob/b1865ea0847a7a86baefe8378b772ecf0b785681/src/System.Net.Http/src/System/Net/Http/HttpRuleParser.cs
        private static readonly string[] dateFormats =
        {
            "ddd, d MMM yyyy H:m:s 'GMT'", // RFC 1123 (r, except it allows both 1 and 01 for date and time)
            "ddd, d MMM yyyy H:m:s", // RFC 1123, no zone - assume GMT
            "d MMM yyyy H:m:s 'GMT'", // RFC 1123, no day-of-week
            "d MMM yyyy H:m:s", // RFC 1123, no day-of-week, no zone
            "ddd, d MMM yy H:m:s 'GMT'", // RFC 1123, short year
            "ddd, d MMM yy H:m:s", // RFC 1123, short year, no zone
            "d MMM yy H:m:s 'GMT'", // RFC 1123, no day-of-week, short year
            "d MMM yy H:m:s", // RFC 1123, no day-of-week, short year, no zone

            "dddd, d'-'MMM'-'yy H:m:s 'GMT'", // RFC 850
            "dddd, d'-'MMM'-'yy H:m:s", // RFC 850 no zone
            "ddd MMM d H:m:s yyyy", // ANSI C's asctime() format

            "ddd, d MMM yyyy H:m:s zzz", // RFC 5322
            "ddd, d MMM yyyy H:m:s", // RFC 5322 no zone
            "d MMM yyyy H:m:s zzz", // RFC 5322 no day-of-week
            "d MMM yyyy H:m:s", // RFC 5322 no day-of-week, no zone
        };

        internal static Dictionary<TKey, TValue> GetValue<TKey, TValue>(this SerializationInfo info, string name, IEqualityComparer<TKey> comparer)
        {
            var items = (Tuple<TKey, TValue>[])info.GetValue(name, typeof(Tuple<TKey, TValue>[]));

            return items.ToDictionary(item => item.Item1, item => item.Item2, comparer);
        }
        internal static void AddValue<TKey, TValue>(this SerializationInfo info, string name, IDictionary<TKey, TValue> dictionary)
        {
            info.AddValue(name, dictionary
                .Select(pair => new Tuple<TKey, TValue>(pair.Key, pair.Value))
                .ToArray(), typeof(Tuple<TKey, TValue>[]));
        }

        /// <summary>
        /// If the Allow header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingAllowHeader(this IDictionary<string, string[]> headers, out ICollection<string> value)
        {
            return headers.TryParseHeaderValues("Allow", headerValues => headerValues, out value);
        }

        /// <summary>
        /// If the Content-Disposition header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingContentDispositionHeader(this IDictionary<string, string[]> headers, out ContentDispositionHeaderValue value)
        {
            return headers.TryParseHeaderValue("Content-Disposition", ContentDispositionHeaderValue.Parse, out value);
        }

        /// <summary>
        /// If the Content-Encoding header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingContentEncodingHeader(this IDictionary<string, string[]> headers, out ICollection<string> value)
        {
            return headers.TryParseHeaderValues("Content-Encoding", headerValues => headerValues, out value);
        }

        /// <summary>
        /// If the Content-Language header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingContentLanguageHeader(this IDictionary<string, string[]> headers, out ICollection<string> value)
        {
            return headers.TryParseHeaderValues("Content-Language", headerValues => headerValues, out value);
        }

        /// <summary>
        /// If the Content-Length header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingContentLengthHeader(this IDictionary<string, string[]> headers, out long? value)
        {
            return headers.TryParseHeaderValue("Content-Length", out value);
        }

        /// <summary>
        /// If the Content-Location header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingContentLocationHeader(this IDictionary<string, string[]> headers, out Uri value)
        {
            return headers.TryParseHeaderValue("Content-Location", headerValue => new Uri(headerValue), out value);
        }

        /// <summary>
        /// If the Content-MD5 header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingContentMd5Header(this IDictionary<string, string[]> headers, out byte[] value)
        {
            return headers.TryParseHeaderValue("Content-MD5", Convert.FromBase64String, out value);
        }

        /// <summary>
        /// If the Content-Range header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingContentRangeHeader(this IDictionary<string, string[]> headers, out ContentRangeHeaderValue value)
        {
            return headers.TryParseHeaderValue("Content-Range", ContentRangeHeaderValue.Parse, out value);
        }

        /// <summary>
        /// If the Content-Type header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingContentTypeHeader(this IDictionary<string, string[]> headers, out MediaTypeHeaderValue value)
        {
            return headers.TryParseHeaderValue("Content-Type", MediaTypeHeaderValue.Parse, out value);
        }

        /// <summary>
        /// If the Expires header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingExpiresHeader(this IDictionary<string, string[]> headers, out DateTimeOffset? value)
        {
            return headers.TryParseHeaderValue("Expires", out value);
        }

        /// <summary>
        /// If the Last-Modified header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParsingLastModifiedHeader(this IDictionary<string, string[]> headers, out DateTimeOffset? value)
        {
            return headers.TryParseHeaderValue("Last-Modified", out value);
        }

        /// <summary>
        /// If the header key exists, the value will be parsed as a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParseHeaderValue(this IDictionary<string, string[]> headers, string key, out DateTimeOffset? value)
        {
            return headers.TryParseHeaderValue(key, headerValue =>
            {
                DateTimeOffset returnValue;
                if (DateTimeOffset.TryParseExact(headerValue, dateFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out returnValue))
                {
                    return returnValue;
                }
                int invalidDate;
                if (int.TryParse(headerValue, out invalidDate) && invalidDate <= 0)
                {
                    return null;
                }
                throw new IndexOutOfRangeException(key);
            },
            out value);
        }

        /// <summary>
        /// If the header key exists, the value will be parsed as a <see cref="long"/>.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParseHeaderValue(this IDictionary<string, string[]> headers, string key, out long? value)
        {
            return headers.TryParseHeaderValue(key, headerValue =>
            {
                long returnValue;
                if (!string.IsNullOrEmpty(headerValue) && long.TryParse(headerValue, out returnValue))
                {
                    return returnValue;
                }
                throw new IndexOutOfRangeException(key);
            },
            out value);
        }

        /// <summary>
        /// If the header key exists, the value will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParseHeaderValue<T>(this IDictionary<string, string[]> headers, string key, Func<string, T> parse, out T value)
        {
            return headers.TryParseHeaderValues(key, headerValues => parse(string.Join("; ", headerValues)), out value);
        }

        /// <summary>
        /// If the header key exists, the values will be parsed.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryParseHeaderValues<T>(this IDictionary<string, string[]> headers, string key, Func<string[], T> parse, out T value)
        {
            string[] headerValues;
            if (headers.TryRemoveHeader(key, out headerValues))
            {
                value = parse(headerValues);
                return true;
            }
            value = default(T);
            return false;
        }

        /// <summary>
        /// Removes a header by key.
        /// </summary>
        /// <returns>True of the key exists.</returns>
        public static bool TryRemoveHeader(this IDictionary<string, string[]> headers, string key, out string[] headerValues)
        {
            var returnValue = headers.TryGetValue(key, out headerValues);
            if (returnValue)
            {
                headers.Remove(key);
            }
            return returnValue;
        }
    }
}
