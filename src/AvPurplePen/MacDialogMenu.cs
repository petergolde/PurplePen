// MacDialogMenu.cs
//
// Keeps the macOS menu bar populated while a dialog is open.
//
// On macOS Avalonia exports the NSApp main menu per window: when a window becomes the key
// window it installs its own NativeMenu (AvnWindow.becomeKeyWindow -> showWindowMenuWithAppMenu),
// and a window with no NativeMenu gets an empty one. Our main menu lives on MainWindow, so
// every dialog used to blank the menu bar down to just the application menu. Real Mac apps
// leave the menu titles in place and grey out the items that don't apply.
//
// Rather than repeating the menu in every dialog's AXAML, a disabled copy of the main window's
// menu is attached to each dialog window as it opens, via the global Window.WindowOpenedEvent
// class handler installed by Install().
//
// The copy keeps the menu bar's own submenus rather than being a flat row of disabled titles,
// because AppKit only renders a menu bar item that actually has a submenu. Below that level the
// submenus are dropped, which is what lets every item show as greyed. The result matches how a
// native Mac app behaves with a modal dialog up: the menu titles stay live and open, and the
// commands inside them are greyed.
//
// Note this does not cover the platform file open/save pickers: those are not Avalonia windows,
// so the owning window simply resigns key status and Avalonia's AvnWindow.windowDidResignKey
// unconditionally drops back to the application menu. That path has no managed hook.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace AvPurplePen
{
    /// <summary>
    /// Installs a disabled copy of the main menu on dialog windows so that the macOS menu bar
    /// keeps its contents while a dialog is showing. No-op on Windows and Linux, where the main
    /// menu is rendered inside the main window by NativeMenuBar instead.
    /// </summary>
    internal static class MacDialogMenu
    {
        /// <summary>
        /// Registers the class handler that attaches the disabled menu copy to each dialog window
        /// as it opens. Call once during application startup.
        /// </summary>
        public static void Install()
        {
            if (!OperatingSystem.IsMacOS())
                return;

            Window.WindowOpenedEvent.AddClassHandler<Window>(OnWindowOpened);
        }

        /// <summary>
        /// Called for every window as it opens. Attaches a disabled copy of the main menu to any
        /// window that doesn't carry a menu of its own.
        /// </summary>
        /// <param name="window">The window being opened.</param>
        /// <param name="e">Event arguments (unused).</param>
        private static void OnWindowOpened(Window window, RoutedEventArgs e)
        {
            // The main window owns the real menu, so it is excluded here. Window.Owner is not
            // usable to spot dialogs: WindowOpenedEvent is raised early in Window.ShowCore,
            // before the owner is assigned, so it is still null at this point.
            if (NativeMenu.GetMenu(window) != null)
                return;

            NativeMenu? mainMenu = FindMainMenu();
            if (mainMenu == null)
                return;         // Only the welcome screen is up; there is no menu to copy.

            NativeMenu.SetMenu(window, CloneDisabled(mainMenu, menuBar: true));
        }

        /// <summary>
        /// Finds the menu to copy: the one belonging to whichever open window carries a menu,
        /// which is the main window whenever there is an event loaded. Returns null when no open
        /// window has a menu, i.e. while the welcome screen is the only window.
        /// </summary>
        /// <returns>The main window's menu, or null if there isn't one.</returns>
        private static NativeMenu? FindMainMenu()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;

            foreach (Window window in desktop.Windows) {
                NativeMenu? menu = NativeMenu.GetMenu(window);
                if (menu != null)
                    return menu;
            }

            return null;
        }

        /// <summary>
        /// Copies a menu, disabling every item and dropping the commands, so that nothing in it
        /// can be invoked. Copied fresh for each dialog, so the item text follows the current UI
        /// language, which can change while the program is running.
        /// </summary>
        /// <param name="source">The menu to copy.</param>
        /// <param name="menuBar">
        /// True for the menu bar itself, false for a menu dropping down from it. Submenus are
        /// copied only for the menu bar, because AppKit renders a menu bar item solely when it
        /// has one. Everywhere below that they are dropped: an item that keeps its submenu can't
        /// be greyed out at all, since Avalonia's validateMenuItem: reports any item that has a
        /// submenu as enabled no matter what IsEnabled says. Dropping it turns a submenu item
        /// such as View > Map Intensity into a plain disabled command, which is the point.
        /// </param>
        /// <returns>The disabled copy.</returns>
        private static NativeMenu CloneDisabled(NativeMenu source, bool menuBar)
        {
            NativeMenu clone = new NativeMenu();

            foreach (NativeMenuItemBase item in source.Items) {
                // Separators must be tested for first: NativeMenuItemSeparator derives from
                // NativeMenuItem, so the test below would otherwise match them too and copy them
                // as ordinary items whose header is the "-" their constructor sets.
                if (item is NativeMenuItemSeparator) {
                    clone.Add(new NativeMenuItemSeparator());
                }
                else if (item is NativeMenuItem menuItem) {
                    // Gesture is deliberately not copied. A greyed item still registers its key
                    // equivalent with AppKit, and there is no reason to let the menu bar see
                    // keystrokes -- Cmd+C and friends -- that belong to the dialog's own controls.
                    NativeMenuItem copy = new NativeMenuItem {
                        Header = menuItem.Header,
                        Icon = menuItem.Icon,
                        IsVisible = menuItem.IsVisible,
                        ToggleType = menuItem.ToggleType,
                        IsChecked = menuItem.IsChecked,
                        IsEnabled = false,
                    };

                    if (menuBar && menuItem.Menu != null)
                        copy.Menu = CloneDisabled(menuItem.Menu, menuBar: false);

                    clone.Add(copy);
                }
            }

            return clone;
        }
    }
}
