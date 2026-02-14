using System;
using System.Net.Http;

namespace PurplePen.Livelox
{
    class LiveloxApiRequestContext
    {
        public HttpRequestMessage CreateRequest()
        {
            Request = RequestCreator();
            return Request;
        }

        public Func<HttpRequestMessage> RequestCreator { get; set; }
        public HttpRequestMessage Request { get; set; }
        public int RetryCount { get; set; }
    }
}
