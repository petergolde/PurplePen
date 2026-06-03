using Avalonia.Threading;
using PurplePen.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace AvPurplePen
{
    public class EventDispatcherService : IEventDispatcherService
    {
        public void PostMessage(Action action)
        {
            Dispatcher.UIThread.Post(action);
        }

        // TODO: This is highly problematic, and could lead to some severe issues
        // on non-Windowes platforms. We should try to refactor to using async and potentially
        // Task.Yield or Task.Delay(1) to allow the UI thread to process messages without using a nested message loop.
        public void ProcessPendingMessages()
        {
            // WinForms Application.DoEvents() equivalent.
            //
            // Dispatcher.UIThread.RunJobs() only drains the dispatcher's own
            // queue — it does NOT pump the underlying OS message queue, so
            // input events (e.g. a Cancel button click sitting in the win32
            // queue) never get translated into Avalonia events. That's
            // enough for paint and binding updates (which queue dispatcher
            // operations directly) but not for input.
            //
            // PushFrame enters a nested dispatcher loop that actually runs
            // the dispatcher (pulling OS messages, dispatching input,
            // running render/layout/bindings) until told to stop. We post a
            // Background-priority "stop" so the loop exits once everything
            // currently queued — including any input that just arrived from
            // the OS while we were busy — has been processed.
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.UIThread.Post(
                () => {
                    frame.Continue = false;
                },
                DispatcherPriority.Background);
            Dispatcher.UIThread.PushFrame(frame);
        }
    }
}
