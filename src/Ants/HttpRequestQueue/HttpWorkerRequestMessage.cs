using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Ants.HttpRequestQueue
{
    internal class HttpWorkerRequestMessage : HttpWorkerRequest
    {
        private long? getLength()
        {
            var lengthAsNullable = Parent.ContentLength;
            if (lengthAsNullable.HasValue)
            {
                return lengthAsNullable.Value;
            }

            if (RequestHeaders.TryGetValue("Content-Length", out string lengthAsString) &&
                long.TryParse(lengthAsString, out long length) &&
                length >= 0)
            {
                return length;
            }

            try
            {
                return Parent.RequestStream.Length;
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            return null;
        }
        private readonly byte[] preLoad;

        public HttpWorkerRequestMessage(Message parent)
        {
            HttpMethod = parent.HttpMethod;
            HttpVersion = parent.HttpVersion;
            Message = parent;
            Parent = parent;
            RequestHeaders = parent.RequestHeadersAsTuples().ToDictionary(pair => pair.Item1, pair => pair.Item2, StringComparer.OrdinalIgnoreCase);
            Url = parent.Url;

            var length = getLength();

            if (length.HasValue)
            {
                RequestHeaders["Content-Length"] = length.Value.ToString();
            }
            else
            {
                return;
            }

            preLoad = new byte[length.Value];
            var offset = 0;
            while (offset < length)
            {
                offset += parent.RequestStream.Read(preLoad, offset, (int)length.Value - offset);
            }
        }
        //byte[] GetQueryStringRawBytes()

        public Dictionary<string, string[]> ResponseHeaders => Message.ResponseHeadersAsTuples().ToDictionary(pair => pair.Item1, pair => pair.Item2, StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> RequestHeaders { get; }
        public Message Parent { get; private set; }
        public Uri Url { get; }
        public override bool IsEntireEntityBodyIsPreloaded()
        {
            return preLoad != null;
        }
        public override byte[] GetPreloadedEntityBody()
        {
            return preLoad;
        }
        public override byte[] GetQueryStringRawBytes()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return Encoding.UTF8.GetBytes(GetQueryString());
        }
        public override int GetLocalPort()
        {
            return Url.Port;
        }
        public override int GetTotalEntityBodyLength()
        {
            var length = getLength();
            return length.HasValue ? (int)length.Value : 0;
        }
        public override int GetRemotePort()
        {
            return Url.Port;
        }
        public override int ReadEntityBody(byte[] buffer, int size)
        {
            return Parent?.RequestStream.Read(buffer, 0, size) ?? 0;
        }
        public override int ReadEntityBody(byte[] buffer, int offset, int size)
        {
            return Parent?.RequestStream.Read(buffer, offset, size) ?? 0;
        }
        public override string GetHttpVerbName()
        {
            return HttpMethod;
        }
        public override string GetHttpVersion()
        {
            return HttpVersion;
        }
        public override string GetKnownRequestHeader(int index)
        {
            return GetUnknownRequestHeader(GetKnownRequestHeaderName(index));
        }
        public override string GetUnknownRequestHeader(string name)
        {
            return RequestHeaders.TryGetValue(name, out string value) ? value : null;
        }
        public override string GetLocalAddress()
        {
            return "127.0.0.1";
        }
        public override string GetQueryString()
        {
            return string.IsNullOrEmpty(Url.Query) ? "" : Url.Query.Substring(1);
        }
        public override string GetRawUrl()
        {
            return Url.PathAndQuery;
        }
        public override string GetRemoteAddress()
        {
            return "127.0.0.1";
        }
        public override string GetUriPath()
        {
            return Url.LocalPath;
        }
        public override string[][] GetUnknownRequestHeaders()
        {
            return RequestHeaders
                .Where(pair => GetKnownRequestHeaderIndex(pair.Key) != -1)
                .Select(pair => new[]
                {
                    pair.Key,
                    pair.Value
                })
                .ToArray();
        }
        public override void EndOfRequest()
        {
            var parent = Parent;
            Parent = null;
            parent.EndOfRequest();
        }
        public override void FlushResponse(bool finalFlush)
        {
            Parent?.ResponseStream.Flush();
        }
        public override void SendCalculatedContentLength(int contentLength)
        {
            Parent?.TryAddResponseHeader("Content-Length", contentLength.ToString());
        }
        public override void SendKnownResponseHeader(int index, string value)
        {
            Parent?.SetResponseHeader(GetKnownResponseHeaderName(index), value);
        }
        public override void SendResponseFromMemory(byte[] data, int length)
        {
            Parent?.ResponseStream.Write(data, 0, length);
        }
        public override void SendStatus(int statusCode, string statusDescription)
        {
            var parent = Parent;
            if (parent == null)
            {
                return;
            }

            parent.HttpStatusCode = (HttpStatusCode)statusCode;
            parent.ReasonPhrase = statusDescription;
        }
        public override void SendUnknownResponseHeader(string name, string value)
        {
            Parent?.SetResponseHeader(name, value);
        }
        public override void SendResponseFromFile(IntPtr handle, long offset, long length)
        {
            throw new NotSupportedException();
        }
        public override void SendResponseFromFile(string filename, long offset, long length)
        {
            using (var stream = File.OpenRead(filename))
            {
                var buffer = new byte[length];
                var read = stream.Read(buffer, (int)offset, buffer.Length);
                Parent?.ResponseStream.Write(buffer, 0, read);
            }
        }
        public readonly Message Message;
        public string HttpMethod { get; }
        public string HttpVersion { get; }
    }
}
