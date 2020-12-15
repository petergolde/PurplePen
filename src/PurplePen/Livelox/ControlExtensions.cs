using System;
using System.Windows.Forms;

namespace PurplePen.Livelox
{
    public static class ControlExtensions
    {
        public static void InvokeOnUiThread(this Control control, Action action)
        {
            control?.Invoke((MethodInvoker) delegate
            {
                action();
            });
        }
    }
}