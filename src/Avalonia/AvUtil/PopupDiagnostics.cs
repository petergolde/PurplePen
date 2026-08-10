using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Text;

namespace AvUtil
{
    // Diagnostic logging for popup placement problems, in particular the Linux/X11 bug where
    // popups and menus appear displaced when the main window is on a secondary monitor.
    //
    // Avalonia positions a desktop popup (Popup, Flyout, MenuItem dropdown) by taking the anchor
    // rectangle in window-relative logical pixels, multiplying by the parent window's scaling, then
    // translating by the parent's client-area origin in screen pixels (PointToScreen). Finally it
    // constrains the result to the bounds of whichever Screen contains that point. That requires
    // three values -- PointToScreen, the scaling factor, and the Screen bounds list -- to all agree
    // on the same pixel coordinate space. When they disagree the error appears only once the window
    // moves off the primary monitor, because on the primary monitor the screen origin is (0, 0) and
    // the discrepancy multiplies out to nothing.
    //
    // This class logs all three values plus the popup's actual resulting position, so the
    // expected-vs-actual delta can be compared against the monitor layout reported by
    // "xrandr --listactivemonitors".
    //
    // Logging is off unless the PPEN_POPUPDIAG environment variable is set to a non-empty value:
    //     PPEN_POPUPDIAG=1 ./PurplePen
    // Output goes to stdout.
    public static class PopupDiagnostics
    {
        private const string EnvironmentVariable = "PPEN_POPUPDIAG";

        private static readonly bool enabled =
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariable));

        // True if popup diagnostic logging is turned on. Callers should test this before doing any
        // work to gather diagnostic information, so that the instrumentation costs nothing when off.
        public static bool Enabled => enabled;

        // Logs the placement of a popup that has just been opened.
        //   label:           identifies the call site in the log (e.g. "MapViewer tooltip").
        //   placementTarget: the control the popup is anchored to -- the popup's PlacementTarget.
        //   offset:          the popup's HorizontalOffset/VerticalOffset, in logical pixels relative
        //                    to the top left of placementTarget.
        //   popupContent:    any visual inside the popup. Its TopLevel is the popup's own window,
        //                    which is what gets positioned; that position is the "actual" value.
        //
        // Call this after the popup has been opened; the popup has no top level before that. The
        // popup is not positioned synchronously when it is opened, so the actual reading is taken on
        // a later dispatcher pass. This method returns immediately.
        //
        // A non-zero delta near a screen edge can be legitimate: Avalonia flips or slides a popup
        // that would otherwise fall off the screen. Test well inside the monitor's bounds.
        public static void LogPlacement(string label, Visual placementTarget, Point offset, Visual popupContent)
        {
            if (!enabled)
                return;

            // Capture the expected position now, while the caller's state is still current.
            PixelPoint? parentOrigin = TryPointToScreen(placementTarget, default);
            PixelPoint? expected = TryPointToScreen(placementTarget, offset);

            Dispatcher.UIThread.Post(() => {
                Report(label, placementTarget, offset, popupContent, parentOrigin, expected);
            }, DispatcherPriority.Loaded);
        }

        // Builds and writes the diagnostic report. Runs after layout, so the popup's top level has
        // been created and moved into its final position.
        private static void Report(string label, Visual placementTarget, Point offset, Visual popupContent,
                                   PixelPoint? parentOrigin, PixelPoint? expected)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("=== popup placement: " + label + " ===");
            builder.AppendLine("  session:      XDG_SESSION_TYPE=" +
                               (Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "<unset>") +
                               "  WAYLAND_DISPLAY=" +
                               (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") ?? "<unset>"));

            TopLevel? parentTopLevel = TopLevel.GetTopLevel(placementTarget);
            if (parentTopLevel == null) {
                builder.AppendLine("  parent window: <none -- placement target is not in a window>");
                Console.WriteLine(builder.ToString());
                return;
            }

            builder.AppendLine("  parent window: RenderScaling=" + parentTopLevel.RenderScaling +
                               "  DesktopScaling=" + DescribeDesktopScaling(parentTopLevel) +
                               "  ClientSize=" + parentTopLevel.ClientSize);
            if (parentTopLevel is Window parentWindow)
                builder.AppendLine("                 Window.Position=" + parentWindow.Position);
            builder.AppendLine("  placement target PointToScreen(0,0) = " + Describe(parentOrigin));

            AppendScreens(builder, parentTopLevel, placementTarget);

            // The popup's own top level is the window that Avalonia actually moved.
            TopLevel? popupTopLevel = TopLevel.GetTopLevel(popupContent);
            PixelPoint? actual = popupTopLevel == null ? null : TryPointToScreen(popupTopLevel, default);

            builder.AppendLine("  offset (logical px, from target top left) = " + offset);
            builder.AppendLine("  expected popup origin = " + Describe(expected));
            builder.AppendLine("  actual   popup origin = " + Describe(actual));
            if (popupTopLevel != null)
                builder.AppendLine("  popup RenderScaling = " + popupTopLevel.RenderScaling);

            if (expected.HasValue && actual.HasValue) {
                PixelPoint delta = new PixelPoint(actual.Value.X - expected.Value.X,
                                                  actual.Value.Y - expected.Value.Y);
                builder.AppendLine("  DELTA (actual - expected) = " + delta +
                                   (delta == default ? "   <- correct" : "   <- MISPLACED"));
            }

            Console.WriteLine(builder.ToString());
        }

        // Appends the screen layout as Avalonia sees it, and which screen it believes the window is
        // on. Compare the bounds against "xrandr --listactivemonitors": if they disagree, the
        // coordinate spaces have diverged and every popup on a non-primary monitor will be displaced.
        private static void AppendScreens(StringBuilder builder, TopLevel topLevel, Visual placementTarget)
        {
            Screens? screens = topLevel.Screens;
            if (screens == null) {
                builder.AppendLine("  screens: <not available>");
                return;
            }

            builder.AppendLine("  screens (" + screens.ScreenCount + "):");
            foreach (Screen screen in screens.All) {
                builder.AppendLine("    " + (screen.DisplayName ?? "<unnamed>") +
                                   " bounds=" + screen.Bounds +
                                   " workArea=" + screen.WorkingArea +
                                   " scaling=" + screen.Scaling +
                                   " primary=" + screen.IsPrimary);
            }

            Screen? current = screens.ScreenFromVisual(placementTarget);
            builder.AppendLine("    window is on: " +
                               (current == null ? "<unknown>" : (current.DisplayName ?? current.Bounds.ToString())));
        }

        // Returns the DesktopScaling of a top level, or a placeholder if it is not a Window.
        private static string DescribeDesktopScaling(TopLevel topLevel)
        {
            return topLevel is Window window ? window.DesktopScaling.ToString() : "<n/a>";
        }

        // Converts a point to screen coordinates, returning null if the visual has no top level yet.
        private static PixelPoint? TryPointToScreen(Visual visual, Point point)
        {
            try {
                return visual.PointToScreen(point);
            }
            catch (Exception) {
                // PointToScreen throws if the visual is not attached to a top level.
                return null;
            }
        }

        // Formats a nullable pixel point for the log.
        private static string Describe(PixelPoint? point)
        {
            return point.HasValue ? point.Value.ToString() : "<unavailable>";
        }
    }
}
