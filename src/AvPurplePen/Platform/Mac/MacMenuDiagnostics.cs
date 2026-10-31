// MacMenuDiagnostics.cs
//
// Diagnostic logging for a rare macOS hang in which the application gets stuck inside AppKit's
// menu tracking loop. When it happens the menu bar titles still respond, but no menu appears, the
// main window ignores all input, and nothing short of Force Quit gets out of it. A sample of the
// stuck process showed the main thread idle in -[NSMenuTrackingSession startRunningMenuEventLoop:],
// started by the system menu bar (MenuBarClientCore) asking the application to open a menu, and
// the window list showed a popup-menu window that was not on screen.
//
// The suspected cause is Avalonia replacing NSApp.mainMenu on every key-window change (see
// MacMenuUtilities.cs), leaving the out-of-process menu bar with a stale copy of the menu, so that
// a click on the menu bar opens a menu that is no longer part of the main menu. This logging is
// meant to confirm or rule that out the next time the hang occurs. It records:
//
//   - key-window changes, application activation, and Avalonia windows opening and closing;
//   - every change of NSApp.mainMenu, with its top-level titles;
//   - menu tracking beginning and ending, with the menu tracked and whether it is still part of
//     the main menu;
//   - Purple Pen's own changes to the menu bar (logged from MacMenuUtilities);
//   - while a menu has been tracked for more than a couple of seconds, a snapshot of the state
//     every few seconds: the tracked menu, the main menu, the key, main and modal windows, and
//     the application's windows as the window server sees them, including whether any menu
//     window is actually on screen.
//
// The snapshots work because the hang does not block the main thread: it sits idle in the menu
// tracking loop, and timers scheduled in the common run loop modes still fire there.
//
// The log goes to ~/Library/Logs/PurplePen/menu-diagnostics.log, flushed line by line so that a
// Force Quit loses nothing. It is moved aside to menu-diagnostics.old.log when it gets large.
// Logging never throws; any failure just loses that line.
//
// Only compiled when targeting macOS (see AvPurplePen.csproj).

using AppKit;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Foundation;
using ObjCRuntime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AvPurplePen.Platform.Mac
{
    /// <summary>
    /// Writes a diagnostic log of menu bar, menu tracking and key-window activity, to help find
    /// the cause of the menu tracking hang described at the top of this file.
    /// </summary>
    internal static class MacMenuDiagnostics
    {
        // The log is moved aside when it grows beyond this size, checked at startup.
        private const long MaxLogSize = 4 * 1024 * 1024;

        // How often the main menu is checked for having been replaced, in seconds.
        private const double MainMenuPollInterval = 0.1;

        // How often the heartbeat checks for a menu that has been tracked for a long time, in seconds.
        private const double HeartbeatInterval = 1.0;

        // How long after tracking begins the menu's window is checked for being on screen, in seconds.
        private const double MenuWindowCheckDelay = 0.5;

        // How long a menu must be tracked before snapshots are taken, in seconds.
        private const double StuckThreshold = 2.0;

        // Seconds between snapshots, for the first few snapshots of an episode and then afterwards.
        private const double SnapshotIntervalEarly = 3.0;
        private const double SnapshotIntervalLate = 30.0;
        private const int EarlySnapshotCount = 20;

        // The window server's level for menus (kCGPopUpMenuWindowLevel).
        private const int PopUpMenuWindowLevel = 101;

        // kCGWindowListOptionAll for CGWindowListCopyWindowInfo.
        private const uint WindowListOptionAll = 0;

        // The run loop mode AppKit uses while tracking a menu, a drag or a resize.
        private const string EventTrackingRunLoopMode = "NSEventTrackingRunLoopMode";

        private static StreamWriter? writer;
        private static bool installed;

        // Measures elapsed time for the tracking and snapshot logic.
        private static readonly Stopwatch clock = Stopwatch.StartNew();

        // The observers and timers, kept so they stay registered.
        private static readonly List<NSObject> observers = new List<NSObject>();
        private static NSTimer? mainMenuPollTimer, heartbeatTimer;

        // The main menu as last seen by the poll: its handle and number of items.
        private static IntPtr lastMainMenuHandle;
        private static nint lastMainMenuCount = -1;

        // The menu being tracked, how deeply tracking notifications are nested, and when tracking began.
        private static NSMenu? trackedMenu;
        private static int trackingDepth;
        private static double trackingBegan;

        // When the run loop was first seen in event tracking mode with no menu being tracked, or null.
        private static double? trackingModeSince;

        // The snapshot schedule for the current stuck episode.
        private static double nextSnapshotAt;
        private static int snapshotsThisEpisode;

        // Every menu seen as NSApp.mainMenu, by handle. For a menu bar menu, the menu tracking
        // notifications name the main menu itself rather than the submenu that drops down, so a
        // tracked menu found here but no longer the main menu is one that was swapped out.
        private static readonly HashSet<IntPtr> seenMainMenus = new HashSet<IntPtr>();

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

        /// <summary>
        /// Opens the log and registers the notifications and timers. Call once at startup, on the
        /// main thread, after NSApplication.Init().
        /// </summary>
        public static void Install()
        {
            if (installed)
                return;
            installed = true;

            try {
                OpenLog();

                Log($"===== Purple Pen started: pid {Environment.ProcessId}, macOS {NSProcessInfo.ProcessInfo.OperatingSystemVersionString}");

                observers.Add(NSWindow.Notifications.ObserveDidBecomeKey((sender, e) =>
                    Guard(() => OnKeyWindowChanged("became key", e.Notification.Object as NSWindow))));
                observers.Add(NSWindow.Notifications.ObserveDidResignKey((sender, e) =>
                    Guard(() => OnKeyWindowChanged("resigned key", e.Notification.Object as NSWindow))));
                observers.Add(NSApplication.Notifications.ObserveDidBecomeActive((sender, e) =>
                    Log("Application became active")));
                observers.Add(NSApplication.Notifications.ObserveDidResignActive((sender, e) =>
                    Log("Application resigned active")));
                observers.Add(NSMenu.Notifications.ObserveDidBeginTracking((sender, e) =>
                    OnBeginTracking(e.Notification.Object as NSMenu)));
                observers.Add(NSMenu.Notifications.ObserveDidEndTracking((sender, e) =>
                    OnEndTracking(e.Notification.Object as NSMenu)));

                Window.WindowOpenedEvent.AddClassHandler<Window>(OnAvaloniaWindowOpened);
                Window.WindowClosedEvent.AddClassHandler<Window>(OnAvaloniaWindowClosed);

                // Both timers go in the common modes, so that they keep firing while a menu is
                // being tracked -- the very time they are needed.
                mainMenuPollTimer = NSTimer.CreateRepeatingTimer(MainMenuPollInterval, timer => Guard(PollMainMenu));
                NSRunLoop.Main.AddTimer(mainMenuPollTimer, NSRunLoopMode.Common);
                heartbeatTimer = NSTimer.CreateRepeatingTimer(HeartbeatInterval, timer => Guard(Heartbeat));
                NSRunLoop.Main.AddTimer(heartbeatTimer, NSRunLoopMode.Common);
            }
            catch (Exception ex) {
                Log($"Installing menu diagnostics failed: {ex}");
            }
        }

        /// <summary>
        /// Writes a line to the log, with a timestamp. Does nothing if the log could not be
        /// opened, and never throws.
        /// </summary>
        /// <param name="message">The line to write.</param>
        public static void Log(string message)
        {
            if (writer == null)
                return;

            try {
                writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}  {message}");
            }
            catch (Exception) {
                // Diagnostics must never disturb the application.
            }
        }

        /// <summary>
        /// Opens the log file for appending, first moving it aside if it has grown too large.
        /// Leaves writer null if the file cannot be opened.
        /// </summary>
        private static void OpenLog()
        {
            try {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "PurplePen");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "menu-diagnostics.log");

                FileInfo info = new FileInfo(path);
                if (info.Exists && info.Length > MaxLogSize)
                    File.Move(path, Path.Combine(directory, "menu-diagnostics.old.log"), overwrite: true);

                writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8) {
                    AutoFlush = true,
                };
            }
            catch (Exception) {
                writer = null;
            }
        }

        /// <summary>
        /// Runs an action from a timer or notification, logging rather than propagating any exception.
        /// </summary>
        /// <param name="action">The action to run.</param>
        private static void Guard(Action action)
        {
            try {
                action();
            }
            catch (Exception ex) {
                Log($"Menu diagnostics error: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // ==================== Event handlers ====================

        /// <summary>
        /// Called when a window becomes or resigns key, which is when Avalonia swaps the main menu.
        /// </summary>
        /// <param name="change">What happened to the window, for the log.</param>
        /// <param name="window">The window, from the notification.</param>
        private static void OnKeyWindowChanged(string change, NSWindow? window)
        {
            NSMenu? mainMenu = NSApplication.SharedApplication.MainMenu;
            RememberMainMenu(mainMenu);
            Log($"Window {change}: {DescribeWindow(window)}; main menu {DescribeMenuBrief(mainMenu)}");
        }

        /// <summary>
        /// Called when any Avalonia window opens.
        /// </summary>
        /// <param name="window">The window that opened.</param>
        /// <param name="e">Event arguments (unused).</param>
        private static void OnAvaloniaWindowOpened(Window window, RoutedEventArgs e)
        {
            Log($"Avalonia window opened: {window.GetType().Name} \"{window.Title}\"");
        }

        /// <summary>
        /// Called when any Avalonia window closes.
        /// </summary>
        /// <param name="window">The window that closed.</param>
        /// <param name="e">Event arguments (unused).</param>
        private static void OnAvaloniaWindowClosed(Window window, RoutedEventArgs e)
        {
            Log($"Avalonia window closed: {window.GetType().Name} \"{window.Title}\"");
        }

        /// <summary>
        /// Called when a menu starts being tracked. Logs the menu and the state around it, and
        /// schedules a check that the menu's window actually appears.
        /// </summary>
        /// <param name="menu">The menu being tracked, from the notification.</param>
        private static void OnBeginTracking(NSMenu? menu)
        {
            Guard(() => {
                ++trackingDepth;
                if (trackingDepth == 1) {
                    trackedMenu = menu;
                    trackingBegan = clock.Elapsed.TotalSeconds;
                    snapshotsThisEpisode = 0;
                    nextSnapshotAt = trackingBegan + StuckThreshold;
                }

                NSApplication app = NSApplication.SharedApplication;
                Log($"Menu tracking began (depth {trackingDepth}): {DescribeTrackedMenu(menu)}; main menu {DescribeMenuBrief(app.MainMenu)}; " +
                    $"key window {DescribeWindow(app.KeyWindow)}; active {app.Active}");

                NSTimer check = NSTimer.CreateTimer(MenuWindowCheckDelay, timer => Guard(() => {
                    if (trackingDepth > 0 && trackedMenu == menu)
                        Log($"Menu tracking {MenuWindowCheckDelay}s in: menu window on screen: {(IsMenuWindowOnScreen() ? "yes" : "NO")}");
                }));
                NSRunLoop.Main.AddTimer(check, NSRunLoopMode.Common);
            });
        }

        /// <summary>
        /// Called when a menu stops being tracked.
        /// </summary>
        /// <param name="menu">The menu that was tracked, from the notification.</param>
        private static void OnEndTracking(NSMenu? menu)
        {
            Guard(() => {
                double duration = clock.Elapsed.TotalSeconds - trackingBegan;
                trackingDepth = Math.Max(0, trackingDepth - 1);
                Log($"Menu tracking ended (depth {trackingDepth}) after {duration:F2}s: {DescribeMenuBrief(menu)}");
                if (trackingDepth == 0)
                    trackedMenu = null;
            });
        }

        /// <summary>
        /// Called from a timer. Logs the main menu whenever it is replaced or its number of items
        /// changes. There is no notification for either.
        /// </summary>
        private static void PollMainMenu()
        {
            NSMenu? mainMenu = NSApplication.SharedApplication.MainMenu;
            IntPtr handle = mainMenu == null ? IntPtr.Zero : (IntPtr)mainMenu.Handle;
            nint count = mainMenu?.Count ?? -1;
            RememberMainMenu(mainMenu);

            if (handle != lastMainMenuHandle || count != lastMainMenuCount) {
                lastMainMenuHandle = handle;
                lastMainMenuCount = count;
                Log($"Main menu is now {DescribeMenuWithItems(mainMenu)}{(trackingDepth > 0 ? " (while a menu is being tracked)" : "")}");
            }
        }

        /// <summary>
        /// Records a menu as having been the main menu, so that a tracked menu can later be
        /// recognised as an earlier main menu. Handles of freed menus may be reused, which could
        /// rarely make a menu look like an earlier main menu when it was not.
        /// </summary>
        /// <param name="mainMenu">The current main menu, or null.</param>
        private static void RememberMainMenu(NSMenu? mainMenu)
        {
            if (mainMenu != null)
                seenMainMenus.Add((IntPtr)mainMenu.Handle);
        }

        /// <summary>
        /// Called from a timer every second. Takes a snapshot of the state when a menu has been
        /// tracked for a long time, or when the run loop has been in event tracking mode for a
        /// long time without any menu tracking notification.
        /// </summary>
        private static void Heartbeat()
        {
            double now = clock.Elapsed.TotalSeconds;
            string? mode = NSRunLoop.Current.CurrentMode?.ToString();

            // A fallback in case the tracking notifications are not posted. Event tracking mode
            // is also used for window drags and resizes, so this alone does not mean a menu.
            if (trackingDepth == 0 && mode == EventTrackingRunLoopMode) {
                if (trackingModeSince == null) {
                    trackingModeSince = now;
                    snapshotsThisEpisode = 0;
                    nextSnapshotAt = now + StuckThreshold;
                }
            }
            else {
                trackingModeSince = null;
            }

            double? since = trackingDepth > 0 ? trackingBegan : trackingModeSince;
            if (since == null || now < nextSnapshotAt)
                return;

            ++snapshotsThisEpisode;
            nextSnapshotAt = now + (snapshotsThisEpisode < EarlySnapshotCount ? SnapshotIntervalEarly : SnapshotIntervalLate);

            string reason = trackingDepth > 0
                ? $"menu tracked for {now - since.Value:F1}s"
                : $"run loop in event tracking mode for {now - since.Value:F1}s with no menu tracking notification";
            LogSnapshot(reason, mode);
        }

        // ==================== Snapshot ====================

        /// <summary>
        /// Logs the state of the menus and windows, for a menu that has been open a long time.
        /// </summary>
        /// <param name="reason">Why the snapshot is being taken.</param>
        /// <param name="runLoopMode">The run loop's current mode.</param>
        private static void LogSnapshot(string reason, string? runLoopMode)
        {
            NSApplication app = NSApplication.SharedApplication;
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"----- Snapshot #{snapshotsThisEpisode}: {reason}");
            builder.AppendLine($"    Run loop mode: {runLoopMode ?? "(none)"}");
            builder.AppendLine($"    Tracked menu: {DescribeTrackedMenu(trackedMenu)}");
            builder.AppendLine($"    Main menu: {DescribeMenuWithItems(app.MainMenu)}");
            builder.AppendLine($"    Active: {app.Active}; key window {DescribeWindow(app.KeyWindow)}; main window {DescribeWindow(app.MainWindow)}; modal window {DescribeWindow(app.ModalWindow)}");
            builder.AppendLine($"    Menu window on screen: {(IsMenuWindowOnScreen() ? "yes" : "NO")}");

            builder.AppendLine("    AppKit windows:");
            foreach (NSWindow window in app.DangerousWindows) {
                builder.AppendLine($"        {DescribeWindow(window)} visible={window.IsVisible} level={window.Level} frame={window.Frame}");
            }

            builder.AppendLine("    Window server windows:");
            foreach (WindowServerWindow window in GetWindowServerWindows()) {
                builder.AppendLine($"        #{window.Number} layer={window.Layer} onscreen={window.OnScreen} bounds={window.Bounds} \"{window.Name}\"");
            }

            Log(builder.ToString().TrimEnd());
        }

        /// <summary>
        /// Whether any of this application's menu windows is on screen, according to the window server.
        /// </summary>
        /// <returns>True if a window at the popup menu level is on screen.</returns>
        private static bool IsMenuWindowOnScreen()
        {
            foreach (WindowServerWindow window in GetWindowServerWindows()) {
                if (window.Layer == PopUpMenuWindowLevel && window.OnScreen)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// A window belonging to this application, as reported by the window server.
        /// </summary>
        private readonly record struct WindowServerWindow(long Number, long Layer, bool OnScreen, string Bounds, string Name);

        /// <summary>
        /// Gets this application's windows from the window server, which, unlike NSApp.windows,
        /// includes the windows AppKit uses to show menus.
        /// </summary>
        /// <returns>The windows owned by this process.</returns>
        private static List<WindowServerWindow> GetWindowServerWindows()
        {
            List<WindowServerWindow> result = new List<WindowServerWindow>();
            long pid = Environment.ProcessId;

            IntPtr arrayHandle = CGWindowListCopyWindowInfo(WindowListOptionAll, 0);
            if (arrayHandle == IntPtr.Zero)
                return result;

            using (NSArray? array = Runtime.GetNSObject<NSArray>(arrayHandle, true)) {
                if (array == null)
                    return result;

                for (nuint i = 0; i < array.Count; ++i) {
                    NSDictionary? info = array.GetItem<NSDictionary>(i);
                    if (info == null || GetLong(info, "kCGWindowOwnerPID") != pid)
                        continue;

                    NSDictionary? bounds = info["kCGWindowBounds"] as NSDictionary;
                    string boundsText = bounds == null ? "?" :
                        $"{GetLong(bounds, "X")},{GetLong(bounds, "Y")} {GetLong(bounds, "Width")}x{GetLong(bounds, "Height")}";

                    result.Add(new WindowServerWindow(
                        GetLong(info, "kCGWindowNumber"),
                        GetLong(info, "kCGWindowLayer"),
                        GetLong(info, "kCGWindowIsOnscreen") != 0,
                        boundsText,
                        (info["kCGWindowName"] as NSString)?.ToString() ?? ""));
                }
            }

            return result;
        }

        /// <summary>
        /// Reads a number from a window server dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to read from.</param>
        /// <param name="key">The key to read.</param>
        /// <returns>The value, or 0 if it is missing or not a number.</returns>
        private static long GetLong(NSDictionary dictionary, string key)
        {
            return (dictionary[key] as NSNumber)?.Int64Value ?? 0;
        }

        // ==================== Descriptions ====================

        /// <summary>
        /// Describes a window briefly: its number, class and title.
        /// </summary>
        /// <param name="window">The window, or null.</param>
        /// <returns>The description.</returns>
        private static string DescribeWindow(NSWindow? window)
        {
            if (window == null)
                return "(none)";
            return $"#{window.WindowNumber} {window.Class.Name} \"{window.Title}\"";
        }

        /// <summary>
        /// Describes a menu briefly: its identity, title and number of items.
        /// </summary>
        /// <param name="menu">The menu, or null.</param>
        /// <returns>The description.</returns>
        private static string DescribeMenuBrief(NSMenu? menu)
        {
            if (menu == null)
                return "(none)";
            return $"menu 0x{((IntPtr)menu.Handle).ToInt64():x} \"{menu.Title}\" ({menu.Count} items)";
        }

        /// <summary>
        /// Describes a menu along with the titles of its items, marking hidden ones and those
        /// without a submenu.
        /// </summary>
        /// <param name="menu">The menu, or null.</param>
        /// <returns>The description.</returns>
        private static string DescribeMenuWithItems(NSMenu? menu)
        {
            if (menu == null)
                return "(none)";

            List<string> titles = new List<string>();
            foreach (NSMenuItem item in menu.Items) {
                string title = item.IsSeparatorItem ? "---" : $"\"{item.Title}\"";
                if (item.Hidden)
                    title += "(hidden)";
                if (!item.IsSeparatorItem && item.Submenu == null)
                    title += "(no submenu)";
                titles.Add(title);
            }
            return $"{DescribeMenuBrief(menu)} [{string.Join(", ", titles)}]";
        }

        /// <summary>
        /// Describes the menu being tracked, with the chain of menus above it and whether that
        /// chain leads to the current main menu. A menu bar menu that does not lead to the main
        /// menu is one that has been swapped out while, or just before, it was opened.
        /// </summary>
        /// <param name="menu">The menu being tracked, or null.</param>
        /// <returns>The description.</returns>
        private static string DescribeTrackedMenu(NSMenu? menu)
        {
            if (menu == null)
                return "(none)";

            NSMenu? mainMenu = NSApplication.SharedApplication.MainMenu;
            List<string> chain = new List<string>();
            NSMenu root = menu;
            for (NSMenu? current = menu.Supermenu; current != null; current = current.Supermenu) {
                chain.Add(DescribeMenuBrief(current));
                root = current;
            }

            string attachment;
            if (menu == mainMenu)
                attachment = "is the current main menu";
            else if (root == mainMenu)
                attachment = "part of the current main menu";
            else if (seenMainMenus.Contains((IntPtr)root.Handle))
                attachment = $"NOT the current main menu: {(chain.Count == 0 ? "is" : "part of")} an earlier main menu that has since been replaced";
            else if (chain.Count == 0)
                attachment = "has no supermenu and was never the main menu (a context menu, or detached)";
            else
                attachment = "NOT part of the current main menu";

            string above = chain.Count == 0 ? "" : $"; above it: {string.Join(" < ", chain)}";
            return $"{DescribeMenuBrief(menu)}, {attachment}{above}";
        }
    }
}
