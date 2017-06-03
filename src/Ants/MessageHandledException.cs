using System;

namespace Ants
{
    /// <summary>
    /// A exception caused by the ASP.NET application unable to process a request.
    /// </summary>
    public class MessageHandledException : ArgumentException
    {
        /// <summary>
        /// Creates an exception caused by the ASP.NET application unable to process a request.
        /// </summary>
        public MessageHandledException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// The stack trace for the exception thrown by the ASP.NET application.
        /// </summary>
        public string AppDomainStackTrace { get; internal set; }

        /// <summary>
        /// The exception type for the exception thrown by the ASP.NET application.
        /// </summary>
        public string ExceptionType { get; internal set; }
    }
}
