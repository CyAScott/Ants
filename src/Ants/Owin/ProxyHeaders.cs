using System.Collections;
using System.Collections.Specialized;
using Ants.HttpRequestQueue;

// ReSharper disable AssignNullToNotNullAttribute

namespace Ants.Owin
{
    internal class ProxyHeaders : NameValueCollection
    {
        private readonly Message message;

        public ProxyHeaders(HttpWorkerRequestMessage parent)
        {
            message = parent.Message;
        }
        public override IEnumerator GetEnumerator()
        {
            return AllKeys.GetEnumerator();
        }
        public override int Count => message.ResponseHeaderCount;
        public override string Get(int index)
        {
            return Get(AllKeys[index]);
        }
        public override string Get(string name)
        {
            return message.GetResponseHeader(name);
        }
        public override string GetKey(int index)
        {
            return AllKeys[index];
        }
        public override string[] AllKeys => message.ResponseKeys;
        public override string[] GetValues(int index)
        {
            return message.GetResponseHeaderValues(AllKeys[index]);
        }
        public override string[] GetValues(string name)
        {
            return message.GetResponseHeaderValues(name);
        }
        public override void Add(string name, string value)
        {
            message.SetResponseHeader(name, value);
        }
        public override void Clear()
        {
            message.ClearHeaders();
        }
        public override void Remove(string name)
        {
            message.RemoveResponseHeader(name);
        }
        public override void Set(string name, string value)
        {
            message.SetResponseHeader(name, value);
        }

        //HasKeys



        //public override KeysCollection Keys { get; }



    }
}
