using System;
using System.Data;
using System.Net;
using System.Timers;

namespace PurplePen.Livelox
{
    class LiveloxApiCall<T> : IAbortable
    {
        public LiveloxApiRequestContext RequestContext { get; set; }
        public HttpWebResponse Response { get; set; }
        public IAsyncResult ResponseHandle { get; set; }
        public T Result { get; set; }
        public Exception Exception { get; set; }
        public LiveloxApiClient Client { get; set; }
        public bool TimedOut { get; private set; }

        public Action<LiveloxApiCall<T>> Callback { get; set; }

        public bool Success => Exception == null;
        
        private Timer timeoutTimer;

        public void Abort()
        {
            Client?.Abort();
            timeoutTimer?.Dispose();
        }

        public void RegisterTimeout(TimeSpan timeout)
        {
            timeoutTimer = new Timer(timeout.TotalMilliseconds)
            {
                AutoReset = false
            };
            timeoutTimer.Elapsed += TimeoutTimerElapsed;
            timeoutTimer.Start();
        }

        private void TimeoutTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Client?.Abort();
            TimedOut = true;
            var request = RequestContext.Request;
            if (request != null && !request.HaveResponse)
            {
                request.Abort();
            }
            timeoutTimer.Elapsed -= TimeoutTimerElapsed;
            timeoutTimer.Dispose();
        }
    }
}