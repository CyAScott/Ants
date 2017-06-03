#if DEBUG

using System;

#pragma warning disable 1591

namespace Ants
{
    public class Testing : MarshalByRefObject
    {
        private static Testing variables;
        public static Testing Variables
        {
            get => variables ?? (variables = new Testing());
            set => variables = value;
        }

        public bool ApplicationStartCalled { get; set; }
        public bool ApplicationStartCompleted { get; set; }
        public bool SessionStartCalled { get; set; }
        public bool ApplicationBeginRequestCalled { get; set; }
        public bool ApplicationAuthenticateRequestCalled { get; set; }
        public bool ApplicationErrorCalled { get; set; }
        public bool SessionEndCalled { get; set; }
        public bool ApplicationEndCalled { get; set; }
    }
}

#endif