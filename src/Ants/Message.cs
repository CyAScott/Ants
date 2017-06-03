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

namespace Ants
{
    internal class Message : MarshalByRefObject
    {
        private HttpContent GetContent(IDictionary<string, string[]> headers)
        {
            MediaTypeHeaderValue contentType;
            var contentTypeSet = headers.TryParsingContentTypeHeader(out contentType);

            HttpContent returnValue = null;
            var body = ResponseStream.ToArray();

            if (contentTypeSet)
            {
                if (contentType.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
                {
                    //this is not implemented at the moment, mostly because it is extremely unusual to for a server to respond using a multi part form
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

            ICollection<string> allow;
            if (headers.TryParsingAllowHeader(out allow))
            {
                foreach (var method in allow)
                {
                    returnValue.Headers.Allow.Add(method);
                }
            }

            ContentDispositionHeaderValue contentDisposition;
            if (headers.TryParsingContentDispositionHeader(out contentDisposition))
            {
                returnValue.Headers.ContentDisposition = contentDisposition;
            }

            ICollection<string> contentEncoding;
            if (headers.TryParsingContentEncodingHeader(out contentEncoding))
            {
                foreach (var encoding in contentEncoding)
                {
                    returnValue.Headers.ContentEncoding.Add(encoding);
                }
            }

            ICollection<string> contentLanguage;
            if (headers.TryParsingContentLanguageHeader(out contentLanguage))
            {
                foreach (var language in contentLanguage)
                {
                    returnValue.Headers.ContentLanguage.Add(language);
                }
            }

            long? contentLength;
            if (headers.TryParsingContentLengthHeader(out contentLength))
            {
                returnValue.Headers.ContentLength = contentLength;
            }

            Uri contentLocation;
            if (headers.TryParsingContentLocationHeader(out contentLocation))
            {
                returnValue.Headers.ContentLocation = contentLocation;
            }

            byte[] contentMd5;
            if (headers.TryParsingContentMd5Header(out contentMd5))
            {
                returnValue.Headers.ContentMD5 = contentMd5;
            }

            ContentRangeHeaderValue contentRange;
            if (headers.TryParsingContentRangeHeader(out contentRange))
            {
                returnValue.Headers.ContentRange = contentRange;
            }

            DateTimeOffset? expires;
            if (headers.TryParsingExpiresHeader(out expires))
            {
                returnValue.Headers.Expires = expires;
            }

            DateTimeOffset? lastModified;
            if (headers.TryParsingLastModifiedHeader(out lastModified))
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
        public HttpStatusCode HttpStatusCode { get; set; }
        public MemoryStream ResponseStream { get; } = new MemoryStream();
        public Stream RequestStream { get; }
        public TaskCompletionSource<HttpResponseMessage> Task { get; } = new TaskCompletionSource<HttpResponseMessage>();
        public Tuple<string, string>[] RequestHeadersAsTuples()
        {
            return request.Headers
                .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                .GroupBy(pair => pair.Key)
                .Select(group => new Tuple<string, string>(group.Key, string.Join("; ", group.SelectMany(item => item.Value))))
                .ToArray();
        }
        public Uri Url => request.RequestUri;
        public long? ContentLength => request.Content?.Headers?.ContentLength;
        public string HttpMethod => request.Method.Method;
        public string HttpVersion => $"HTTP/{request.Version}";
        public string ReasonPhrase { get; set; }
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

            if (!Task.TrySetResult(returnValue))
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
            ResponseHeaders[name] = value.Split(';').Select(item => item.Trim()).ToArray();
        }
        public void TryAddResponseHeader(string name, string value)
        {
            ResponseHeaders.TryAdd(name, value.Split(';').Select(item => item.Trim()).ToArray());
        }
    }
}
