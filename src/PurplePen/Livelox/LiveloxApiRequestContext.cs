using System;
using System.Net;

namespace PurplePen.Livelox
{
    class LiveloxApiRequestContext
    {
        public HttpWebRequest CreateRequest()
        {
            Request = RequestCreator();
            return Request;
        }

        public Func<HttpWebRequest> RequestCreator { get; set; }
        public HttpWebRequest Request { get; set; }
        public int RetryCount { get; set; }
    }
}