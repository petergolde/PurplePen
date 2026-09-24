// ApplicationIdleService.cs
// Provides an ApplicationIdle event that fires once after input processing settles.
// Registers global class handlers on TopLevel for pointer, keyboard, and text input,
// then posts a single idle callback via Dispatcher.UIThread.Post. Multiple inputs between
// dispatches are coalesced into one idle event. Works across all top-level windows.

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PurplePen.ViewModels;

namespace AvPurplePen
{
    /// <summary>
    /// Static service that provides an ApplicationIdle event, similar to
    /// WinForms Application.Idle. Fires once after input processing settles.
    /// Call Initialize() once at application startup.
    /// </summary>
    public static class ApplicationIdleService
    {
        /// <summary>
        /// Raised once when the application becomes idle after processing input.
        /// </summary>
        public static event EventHandler? ApplicationIdle;

        private static bool initialized = false;

        // True when an idle callback has been posted but not yet executed.
        private static bool idleQueued = false;

        /// <summary>
        /// Initialize the service by registering global class handlers on TopLevel
        /// for all input events. These fire for every TopLevel window in the application.
        /// Must be called once at application startup.
        /// </summary>
        public static void Initialize()
        {
            // Global class handlers fire for all instances of the specified type,
            // so these cover every current and future TopLevel window.
            InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnInput, handledEventsToo: true);
            InputElement.PointerReleasedEvent.AddClassHandler<TopLevel>(OnInput, handledEventsToo: true);
            InputElement.PointerMovedEvent.AddClassHandler<TopLevel>(OnInput, handledEventsToo: true);
            InputElement.PointerWheelChangedEvent.AddClassHandler<TopLevel>(OnInput, handledEventsToo: true);
            InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnInput, handledEventsToo: true);
            InputElement.KeyUpEvent.AddClassHandler<TopLevel>(OnInput, handledEventsToo: true);
            InputElement.TextInputEvent.AddClassHandler<TopLevel>(OnInput, handledEventsToo: true);

            // When any window regains activation (e.g., after a native OS dialog closes),
            // queue an idle event. Native dialogs run their own message loop, so Avalonia
            // never sees their input events — this catches the return from those dialogs.
            WindowBase.IsActiveProperty.Changed.Subscribe(new IsActiveObserver());

            // Choosing a native menu item (the macOS menu bar, or its key equivalents) runs the
            // item's Click/Command from a native callback without any Avalonia input event,
            // so hook the Click event of every NativeMenuItem. NativeMenuItem.Click is a plain
            // CLR event with no class-level equivalent, so the items must be found individually:
            // any item added to a menu from now on is caught by its Parent changing; items already
            // in menus built by XAML before this point are caught by walking the application menu
            // now and each window's menu when it opens.
            NativeMenuItemBase.ParentProperty.Changed.AddClassHandler<NativeMenuItem>(OnNativeMenuItemParentChanged);
            Window.WindowOpenedEvent.AddClassHandler<Window>((window, e) => HookNativeMenu(NativeMenu.GetMenu(window)));
            if (Application.Current != null)
                HookNativeMenu(NativeMenu.GetMenu(Application.Current));

            // Queue an initial idle event so subscribers get notified at startup,
            // matching WinForms Application.Idle behavior. (initialized must be set first,
            // or QueueIdle ignores the call.)
            initialized = true;
            QueueIdle();
        }

        /// <summary>
        /// Hooks the Click event of a native menu item when it is added to a menu.
        /// </summary>
        /// <param name="item">The menu item whose Parent changed.</param>
        /// <param name="e">The property change details.</param>
        private static void OnNativeMenuItemParentChanged(NativeMenuItem item, AvaloniaPropertyChangedEventArgs e)
        {
            if (item.Parent != null)
                HookNativeMenuItem(item);
        }

        /// <summary>
        /// Hooks the Click event of every item in a native menu, including submenus.
        /// </summary>
        /// <param name="menu">The menu to hook; may be null.</param>
        private static void HookNativeMenu(NativeMenu? menu)
        {
            if (menu == null)
                return;

            foreach (NativeMenuItemBase itemBase in menu.Items) {
                if (itemBase is NativeMenuItem item) {
                    HookNativeMenuItem(item);
                    HookNativeMenu(item.Menu);
                }
            }
        }

        /// <summary>
        /// Hooks the Click event of a single native menu item. Safe to call more than once
        /// for the same item; the handler is only ever attached once.
        /// </summary>
        /// <param name="item">The menu item to hook.</param>
        private static void HookNativeMenuItem(NativeMenuItem item)
        {
            item.Click -= OnNativeMenuItemClick;
            item.Click += OnNativeMenuItemClick;
        }

        /// <summary>
        /// Called when any native menu item is chosen. Click is raised just before the item's
        /// Command executes, so the idle callback (posted at ApplicationIdle priority) runs after it.
        /// </summary>
        private static void OnNativeMenuItemClick(object? sender, EventArgs e)
        {
            QueueIdle();
        }

        /// <summary>
        /// Called on any input event on any TopLevel. Queues a single idle callback
        /// if one is not already pending.
        /// </summary>
        private static void OnInput(object? sender, RoutedEventArgs e)
        {
            QueueIdle();
        }

        /// <summary>
        /// Posts a single idle callback if one is not already pending.
        /// </summary>
        public static void QueueIdle()
        {
            if (initialized && !idleQueued) {
                idleQueued = true;
                Dispatcher.UIThread.Post(RaiseIdle, DispatcherPriority.ApplicationIdle);
            }
        }

        /// <summary>
        /// Fires the ApplicationIdle event and resets the queued flag.
        /// </summary>
        private static void RaiseIdle()
        {
            idleQueued = false;
            ApplicationIdle?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Observer for WindowBase.IsActive property changes. Queues an idle event
    /// whenever any window becomes active.
    /// </summary>
    internal class IsActiveObserver : IObserver<AvaloniaPropertyChangedEventArgs<bool>>
    {
        public void OnNext(AvaloniaPropertyChangedEventArgs<bool> value)
        {
            if (value.NewValue.GetValueOrDefault())
                ApplicationIdleService.QueueIdle();
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    public class ApplicationIdleServiceAdapter : IApplicationIdleService
    {
        public void QueueIdleEvent()
        {
            ApplicationIdleService.QueueIdle();
        }
    }
}