using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace AvUtil
{
    /// <summary>
    /// Helper methods for working with Avalonia windows.
    /// </summary>
    public static class WindowUtil
    {
        /// <summary>
        /// Ensures a window is at least the given size (in logical pixels), growing any
        /// dimension that is smaller while never shrinking one that is already larger. The
        /// window expands equally on both sides of its centre, then is nudged to stay within
        /// the current screen's working area (if it ends up larger than the screen, the
        /// top-left is pinned to the working-area origin). Does nothing unless the window is
        /// in the Normal state.
        /// </summary>
        /// <param name="window">The window to resize.</param>
        /// <param name="minWidth">Desired minimum width, in logical pixels.</param>
        /// <param name="minHeight">Desired minimum height, in logical pixels.</param>
        /// <returns>True if the window was resized; false if no change was needed.</returns>
        public static bool ExpandWindow(this Window window, double minWidth, double minHeight)
        {
            // Don't fiddle with a maximized/minimized window.
            if (window.WindowState != WindowState.Normal)
                return false;

            double curW = window.ClientSize.Width;
            double curH = window.ClientSize.Height;
            double newW = Math.Max(curW, minWidth);
            double newH = Math.Max(curH, minHeight);

            if (newW <= curW && newH <= curH)
                return false;   // already large enough in both dimensions

            double scaling = window.RenderScaling;

            // Expand equally about the centre: shift the top-left up/left by half the
            // (physical) growth in each axis. Position is in physical screen pixels.
            int newX = window.Position.X - (int)Math.Round((newW - curW) * scaling / 2);
            int newY = window.Position.Y - (int)Math.Round((newH - curH) * scaling / 2);

            // Keep the window within the current screen's working area (best effort: if it
            // is now larger than the screen, pin the top-left to the working-area origin).
            Screen? screen = window.Screens.ScreenFromWindow(window);
            if (screen != null) {
                PixelRect wa = screen.WorkingArea;
                int physW = (int)Math.Round(newW * scaling);
                int physH = (int)Math.Round(newH * scaling);
                newX = Math.Max(wa.X, Math.Min(newX, wa.X + wa.Width - physW));
                newY = Math.Max(wa.Y, Math.Min(newY, wa.Y + wa.Height - physH));
            }

            window.Width = newW;
            window.Height = newH;
            window.Position = new PixelPoint(newX, newY);
            return true;
        }
    }
}
