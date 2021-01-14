using System;
using System.Net;

namespace PurplePen.Livelox
{
    class OAuth2Exception : Exception
    {
        public HttpStatusCode? StatusCode { get; }

        public OAuth2Exception(HttpStatusCode statusCode, string message, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public OAuth2Exception(Exception innerException) : base($"Livelox OAuth2 error: {innerException.Message}", innerException)
        {
        }
        
        public OAuth2Exception(string message) : base(message)
        {
        }
    }
}