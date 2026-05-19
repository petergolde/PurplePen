using System;
using System.Collections.Generic;
using System.Text;

namespace PurplePen.ViewModels
{
    // Interface to encapsulate posting an action to the Dispatcher.
    public interface IEventDispatcherService
    {
        void PostMessage(Action action);
        void ProcessPendingMessages();
    }
}
