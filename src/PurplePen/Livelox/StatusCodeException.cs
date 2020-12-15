using System;
using System.Net;

namespace PurplePen.Livelox
{
    public class StatusCodeException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public StatusCodeException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }
    }
}