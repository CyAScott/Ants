using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ants.HttpRequestQueue
{
    internal class Message : MarshalByRefObject
    {
        private HttpContent GetContent(IDictionary<string, string[]> headers)
        {
            var contentTypeSet = headers.TryParsingContentTypeHeader(out MediaTypeHeaderValue contentType);

            HttpContent returnValue = null;
            var body = ResponseStream.ToArray();

            if (contentTypeSet)
            {
                if (contentType.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
                {
                    //this is not implemented at the moment, mostly because it is extremely unusual for a server to respond using a multi part form
                    throw new NotImplementedException("Content-Type: multipart/* is not supported as a response.");
                }

                if (Regex.IsMatch(contentType.MediaType, @"^(text\/.+|application\/(x-)?(base64|javascript|json|rtf|xml))$", RegexOptions.IgnoreCase))
                {
                    var encoding = string.IsNullOrEmpty(contentType.CharSet) || contentType.CharSet.IndexOf("utf-8", StringComparison.OrdinalIgnoreCase) == -1 ?
                        Encoding.GetEncoding("ISO-8859-1") : Encoding.UTF8;
                    returnValue = new StringContent(encoding.GetString(body), encoding);
                }
            }

            if (returnValue == null)
            {
                returnValue = new ByteArrayContent(body);
            }

            if (contentTypeSet)
            {
                returnValue.Headers.ContentType = contentType;
            }

            if (headers.TryParsingAllowHeader(out ICollection<string> allow))
            {
                foreach (var method in allow)
                {
                    returnValue.Headers.Allow.Add(method);
                }
            }

            if (headers.TryParsingContentDispositionHeader(out ContentDispositionHeaderValue contentDisposition))
            {
                returnValue.Headers.ContentDisposition = contentDisposition;
            }

            if (headers.TryParsingContentEncodingHeader(out ICollection<string> contentEncoding))
            {
                foreach (var encoding in contentEncoding)
                {
                    returnValue.Headers.ContentEncoding.Add(encoding);
                }
            }

            if (headers.TryParsingContentLanguageHeader(out ICollection<string> contentLanguage))
            {
                foreach (var language in contentLanguage)
                {
                    returnValue.Headers.ContentLanguage.Add(language);
                }
            }

            if (headers.TryParsingContentLengthHeader(out long? contentLength))
            {
                returnValue.Headers.ContentLength = contentLength;
            }

            if (headers.TryParsingContentLocationHeader(out Uri contentLocation))
            {
                returnValue.Headers.ContentLocation = contentLocation;
            }

            if (headers.TryParsingContentMd5Header(out byte[] contentMd5))
            {
                returnValue.Headers.ContentMD5 = contentMd5;
            }

            if (headers.TryParsingContentRangeHeader(out ContentRangeHeaderValue contentRange))
            {
                returnValue.Headers.ContentRange = contentRange;
            }

            if (headers.TryParsingExpiresHeader(out DateTimeOffset? expires))
            {
                returnValue.Headers.Expires = expires;
            }

            if (headers.TryParsingLastModifiedHeader(out DateTimeOffset? lastModified))
            {
                returnValue.Headers.LastModified = lastModified;
            }

            return returnValue;
        }
        private readonly HttpRequestMessage request;

        public Message(HttpRequestMessage httpRequestMessage, Stream stream)
        {
            RequestStream = stream;
            request = httpRequestMessage;
        }
        public ConcurrentDictionary<string, string[]> ResponseHeaders { get; } = new ConcurrentDictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        public ConcurrentQueue<string> Cookies = new ConcurrentQueue<string>();
        public HttpStatusCode HttpStatusCode { get; set; }
        public MemoryStream ResponseStream { get; } = new MemoryStream();
        public Stream RequestStream { get; }
        public TaskCompletionSource<Tuple<HttpResponseMessage, string[]>> Task { get; } = new TaskCompletionSource<Tuple<HttpResponseMessage, string[]>>();
        public Tuple<string, string>[] RequestHeadersAsTuples()
        {
            return request.Headers
                .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                .GroupBy(pair => pair.Key)
                .Select(group => new Tuple<string, string>(group.Key, string.Join("; ", group.SelectMany(item => item.Value))))
                .ToArray();
        }
        public Tuple<string, string[]>[] ResponseHeadersAsTuples()
        {
            return ResponseHeaders
                .Select(pair => new Tuple<string, string[]>(pair.Key, pair.Value))
                .ToArray();
        }
        public Uri Url => request.RequestUri;
        public long? ContentLength => request.Content?.Headers?.ContentLength;
        public string HttpMethod => request.Method.Method;
        public string HttpVersion => $"HTTP/{request.Version}";
        public string ReasonPhrase { get; set; }
        public void ClearHeaders()
        {
            ResponseHeaders.Clear();
        }
        public void EndOfRequest()
        {
            ResponseStream.Close();

            var headers = ResponseHeaders
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            var returnValue = new HttpResponseMessage(HttpStatusCode)
            {
                Content = GetContent(headers),
                ReasonPhrase = ReasonPhrase
            };

            foreach (var pair in headers)
            {
                returnValue.Headers.Add(pair.Key, pair.Value);
            }

            if (!Task.TrySetResult(new Tuple<HttpResponseMessage, string[]>(returnValue, Cookies.ToArray())))
            {
                returnValue.Dispose();
            }
        }
        public void EndOfRequestWithError(string message, string stackTrace, string exceptionType)
        {
            Task.TrySetException(new MessageHandledException(message)
            {
                AppDomainStackTrace = stackTrace,
                ExceptionType = exceptionType
            });
        }
        public void EndOfRequestWithShutDown()
        {
            Task.TrySetException(new InvalidProgramException("The ASP.NET test server shut down before this request could process."));
        }
        public void SetResponseHeader(string name, string value)
        {
            if (string.Equals(name, "Set-Cookie", StringComparison.OrdinalIgnoreCase))
            {
                Cookies.Enqueue(value);
            }
            else
            {
                ResponseHeaders[name] = value.Split(';').Select(item => item.Trim()).ToArray();
            }
        }
        public void TryAddResponseHeader(string name, string value)
        {
            if (string.Equals(name, "Set-Cookie", StringComparison.OrdinalIgnoreCase))
            {
                Cookies.Enqueue(value);
            }
            else
            {
                ResponseHeaders.TryAdd(name, value.Split(';').Select(item => item.Trim()).ToArray());
            }
        }
    }
}
