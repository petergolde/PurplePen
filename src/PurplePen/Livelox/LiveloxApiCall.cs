using System;
using System.Net.Http;
using System.Threading;

namespace PurplePen.Livelox
{
    class LiveloxApiCall<T> : IAbortable, IDisposable
    {
        public LiveloxApiRequestContext RequestContext { get; set; }
        public HttpResponseMessage Response { get; set; }
        public T Result { get; set; }
        public Exception Exception { get; set; }
        public LiveloxApiClient Client { get; set; }
        public bool TimedOut { get; private set; }

        public Action<LiveloxApiCall<T>> Callback { get; set; }

        public bool Success => Exception == null;

        public CancellationTokenSource CancellationSource { get; set; } = new CancellationTokenSource();

        public void Abort()
        {
            Client?.Abort();
            CancellationSource?.Cancel();
        }

        // Dispose managed resources.
        public void Dispose()
        {
            CancellationSource?.Dispose();
            CancellationSource = null;
        }

        public void RegisterTimeout(TimeSpan timeout)
        {
            CancellationSource?.CancelAfter(timeout);
        }

        // Called when the cancellation token fires due to timeout.
        public void MarkTimedOut()
        {
            TimedOut = true;
        }
    }
}
