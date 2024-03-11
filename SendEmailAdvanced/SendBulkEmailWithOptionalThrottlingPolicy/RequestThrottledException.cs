using System;
using System.Runtime.Serialization;

namespace SendEmailPlainText
{
    [Serializable]
    internal class RequestThrottledException : Exception
    {
        public RequestThrottledException()
        {
        }

        public RequestThrottledException(string message) : base(message)
        {
        }

        public RequestThrottledException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}