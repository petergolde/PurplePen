// MacMenuUtilities.cs
//
// Fixes for the macOS menu bar. Everything here is a no-op on Windows and Linux, where the same
// NativeMenu is drawn inside the main window by NativeMenuBar and neither problem arises. Two
// unrelated shortcomings are handled, described in turn below.
//
//
// 1. KEEPING THE MENU BAR POPULATED WHILE A DIALOG IS OPEN
//
// On macOS Avalonia exports the NSApp main menu per window: when a window becomes the key window it
// installs its own NativeMenu (AvnWindow.becomeKeyWindow -> showWindowMenuWithAppMenu), and a window
// with no NativeMenu gets an empty one. Our main menu lives on MainWindow, so every dialog used to
// blank the menu bar down to just the application menu. Real Mac apps leave the menu titles in place
// and grey out the items that don't apply.
//
// Rather than repeating the menu in every dialog's AXAML, a disabled copy of the main window's menu
// is attached to each dialog window as it opens, via the global Window.WindowOpenedEvent class
// handler installed by InstallDialogMenus().
//
// The copy keeps the menu bar's own submenus rather than being a flat row of disabled titles,
// because AppKit only renders a menu bar item that actually has a submenu. Below that level the
// submenus are dropped, which is what lets every item show as greyed. The result matches how a
// native Mac app behaves with a modal dialog up: the menu titles stay live and open, and the
// commands inside them are greyed.
//
// Note this does not cover the platform file open/save pickers: those are not Avalonia windows, so
// the owning window simply resigns key status and Avalonia's AvnWindow.windowDidResignKey
// unconditionally drops back to the application menu. That path has no managed hook.
//
//
// 2. CHANGING THE MENU BAR TITLES WHEN THE UI LANGUAGE CHANGES
//
// The names along the menu bar are not the titles of the menu items; as NSMenu.title documents, "if
// the menu is a submenu of the application's main menu, then the title of the menu appears in the
// menu bar". Avalonia gives that NSMenu its title once, when the item's submenu is first created,
// and never revisits it: a Header change is pushed to the NSMenuItem's own title (which the menu
// bar does not draw), and the exporter's Update only creates or removes submenus, never re-titles
// one. So a menu bar title is fixed for as long as its item proxy lives.
//
// The exporter keys those proxies off the managed NativeMenuItem objects, reusing any it has seen
// before, and an item proxy that already holds a submenu reuses that too -- it re-reads the items
// inside but never re-titles the NSMenu. So a refresh needs two new objects per menu: a new item,
// to get a new item proxy, and a new submenu for it to title, since a NativeMenu cannot be moved to
// a different item (NativeMenuItem assigns NativeMenu.Parent when a menu is attached and never
// clears it, and NativeMenu.ParentProperty is registered read-only, so nothing can detach it).
//
// The items inside the submenu are moved across rather than rebuilt, which keeps every binding they
// carry -- Header, IsVisible, IsChecked, Command -- working. That direction of the relationship is
// cleaned up properly: NativeMenu.ItemsChanged clears Parent on everything it removes.
//
// The items declared in MainWindow.axaml are kept aside as the source of the headers -- they are no
// longer in the menu, but their {resx:Localize} bindings live on and keep their Header current, so
// each language change can build a new set of items from them.
//
//
// 3. RESTORING THE MENU BAR AFTER THE WELCOME SCREEN HANDS OVER
//
// Which of the two callbacks above runs last decides what the menu bar ends up showing, and neither
// guards against the other: showAppMenuOnly does not check whether the window resigning key is
// still key, or whether another window has become key since. When the welcome screen hands over to
// the main window, both fire within a tick or two of each other -- the main window becomes key, the
// welcome screen resigns and closes -- and if the resign lands last the menu bar is left showing
// only the application menu. It is a race, so it only bites sometimes.
//
// Reordering the handoff cannot settle it. Once the main window is key, AppKit will not call
// becomeKeyWindow on it again, so no later Show or Activate re-installs the menu; that is why
// switching to another application and back is what brings it round. Instead ReassertMenuBar()
// re-exports the menu once the dispatcher has gone quiet: the export calls SetMainMenu, which
// re-runs showWindowMenuWithAppMenu when the window is key. Harmless when the menu bar is already
// right, restorative when it is not.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AvPurplePen
{
    /// <summary>
    /// macOS menu bar fixes: a disabled copy of the main menu for dialog windows, and a rebuild of
    /// the menu bar when the UI language changes. No-op on Windows and Linux, where the main menu is
    /// rendered inside the main window by NativeMenuBar instead.
    /// </summary>
    internal static class MacMenuUtilities
    {
        /// <summary>
        /// The top-level items as they were declared in XAML, per menu. Held only for their
        /// Header bindings; the items themselves are swapped out of the menu on the first refresh.
        /// Weak keys, so a menu belonging to a closed window is not kept alive.
        /// </summary>
        private static readonly ConditionalWeakTable<NativeMenu, TopLevelItems> declaredItems = new();

        // ==================== Menus for dialog windows ====================

        /// <summary>
        /// Registers the class handler that attaches the disabled menu copy to each dialog window
        /// as it opens. Call once during application startup.
        /// </summary>
        public static void InstallDialogMenus()
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

        // ==================== Menu bar titles after a language change ====================

        /// <summary>
        /// Rebuilds the menu bar of every open window that has a menu. Call after changing the UI
        /// language, once the localized bindings have been refreshed.
        /// </summary>
        public static void RefreshMenuBar()
        {
            if (!OperatingSystem.IsMacOS())
                return;
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            foreach (Window window in desktop.Windows) {
                NativeMenu? menu = NativeMenu.GetMenu(window);
                if (menu != null)
                    RebuildTopLevelItems(menu);
            }
        }

        /// <summary>
        /// Re-exports a window's menu once the dispatcher is quiet, so that a menu bar left empty
        /// by a key-window race is put back. Call after showing a window that takes over from
        /// another one; on any normal startup this changes nothing that is visible.
        /// </summary>
        /// <param name="window">The window whose menu should be re-exported.</param>
        public static void ReassertMenuBar(Window window)
        {
            if (!OperatingSystem.IsMacOS())
                return;

            // Background priority, so this runs after the windows have finished swapping over and
            // AppKit has delivered the key-window callbacks that cause the trouble. Re-exporting
            // before then would just be overwritten by the late one.
            Dispatcher.UIThread.Post(() => {
                NativeMenu? menu = NativeMenu.GetMenu(window);
                if (menu != null) {
                    // The same rebuild the language change uses. Its purpose there is to re-title
                    // the menus, which is wasted here, but what matters is the re-export it
                    // triggers, and reusing it avoids a second way of provoking one.
                    RebuildTopLevelItems(menu);
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Replaces a menu's top-level items, and the submenu hanging off each one, with new
        /// objects carrying the current header text. That is what makes the exporter build a
        /// freshly titled NSMenu for each one. The contents of the submenus are moved across, so
        /// nothing inside them is rebuilt.
        /// </summary>
        /// <param name="menu">The menu bar to rebuild.</param>
        private static void RebuildTopLevelItems(NativeMenu menu)
        {
            TopLevelItems declared = declaredItems.GetValue(menu, TopLevelItems.Capture);

            // The declared items supply the headers; the items currently in the menu supply the
            // submenus. On the first refresh they are one and the same, and afterwards the menu
            // holds the previous refresh's replacements. They stay in step by position.
            List<NativeMenuItemBase> replacements = new List<NativeMenuItemBase>();
            for (int i = 0; i < declared.Items.Count && i < menu.Items.Count; ++i) {
                NativeMenuItem declaredItem = declared.Items[i];

                NativeMenu? newSubMenu = null;
                if (menu.Items[i] is NativeMenuItem currentItem && currentItem.Menu != null) {
                    NativeMenu oldSubMenu = currentItem.Menu;
                    NativeMenuItemBase[] contents = oldSubMenu.Items.ToArray();

                    // Clearing the old menu first is what releases these items: removal is the
                    // only thing that resets Parent, and the new menu rejects an item that still
                    // has one.
                    oldSubMenu.Items.Clear();

                    newSubMenu = new NativeMenu();
                    foreach (NativeMenuItemBase content in contents) {
                        newSubMenu.Add(content);
                    }
                }

                replacements.Add(new NativeMenuItem {
                    Header = declaredItem.Header,
                    Icon = declaredItem.Icon,
                    IsVisible = declaredItem.IsVisible,
                    IsEnabled = declaredItem.IsEnabled,
                    Menu = newSubMenu,
                });
            }

            menu.Items.Clear();
            foreach (NativeMenuItemBase replacement in replacements) {
                menu.Items.Add(replacement);
            }
        }

        /// <summary>
        /// The originally declared top-level items of one menu, kept for their header bindings.
        /// </summary>
        private sealed class TopLevelItems
        {
            public List<NativeMenuItem> Items { get; }

            private TopLevelItems(List<NativeMenuItem> items)
            {
                Items = items;
            }

            /// <summary>
            /// Records a menu's current top-level items. Called once per menu, on the first
            /// refresh, while the items are still the ones the XAML created.
            /// </summary>
            /// <param name="menu">The menu whose items should be recorded.</param>
            /// <returns>The recorded items.</returns>
            public static TopLevelItems Capture(NativeMenu menu)
            {
                return new TopLevelItems(menu.Items.OfType<NativeMenuItem>().ToList());
            }
        }
    }
}
