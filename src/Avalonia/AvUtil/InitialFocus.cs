// InitialFocus.cs
//
// An attached property that gives a control the keyboard focus when it appears,
// so initial focus can be set in markup instead of code-behind.
// (Avalonia has no public equivalent of WPF's FocusManager.FocusedElement.)
//
// Usage in AXAML:
//   <TextBox avutil:InitialFocus.IsEnabled="True" ... />
//
// The control is focused when:
//   - its window opens (if it is in a window that has not yet been shown);
//   - it is attached to the visual tree of an already-open window (for example,
//     a view swapped into a ContentControl); or
//   - its own IsVisible changes to true in an already-open window (for example,
//     a wizard page shown by toggling IsVisible).
//
// If the control itself can't take focus (for example, a UserControl wrapping a
// TreeView or ComboBox, or a whole wizard page), the first focusable, visible
// control inside it is focused instead.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace AvUtil
{
    /// <summary>
    /// Attached property that focuses a control when its window opens, when the
    /// control is attached to the visual tree of an already-open window, or when the
    /// control becomes visible in an already-open window.
    /// </summary>
    public static class InitialFocus
    {
        /// <summary>
        /// When true on an InputElement, that element receives the focus when it
        /// appears (see the class description).
        /// </summary>
        public static readonly AttachedProperty<bool> IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<InputElement, bool>("IsEnabled", typeof(InitialFocus));

        /// <summary>
        /// Registers the change handler for the IsEnabled attached property.
        /// </summary>
        static InitialFocus()
        {
            IsEnabledProperty.Changed.AddClassHandler<InputElement>(OnIsEnabledChanged);
        }

        /// <summary>
        /// Gets whether the element should receive the initial focus.
        /// </summary>
        /// <param name="element">The element to query.</param>
        public static bool GetIsEnabled(InputElement element)
        {
            return element.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// Sets whether the element should receive the initial focus.
        /// </summary>
        /// <param name="element">The element to set the property on.</param>
        /// <param name="value">True to focus the element when it appears.</param>
        public static void SetIsEnabled(InputElement element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// Hooks or unhooks the element's AttachedToVisualTree and PropertyChanged events
        /// when the property changes. If the element is already in a visual tree,
        /// schedules the focus now.
        /// </summary>
        /// <param name="element">The element whose property changed.</param>
        /// <param name="e">Details of the property change.</param>
        private static void OnIsEnabledChanged(InputElement element, AvaloniaPropertyChangedEventArgs e)
        {
            element.AttachedToVisualTree -= Element_AttachedToVisualTree;
            element.PropertyChanged -= Element_PropertyChanged;

            if (e.NewValue is true) {
                element.AttachedToVisualTree += Element_AttachedToVisualTree;
                element.PropertyChanged += Element_PropertyChanged;
                FocusWhenReady(element);   // does nothing if not yet in a visual tree
            }
        }

        /// <summary>
        /// Schedules the focus when the element joins a visual tree.
        /// </summary>
        private static void Element_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is InputElement element)
                FocusWhenReady(element);
        }

        /// <summary>
        /// Schedules the focus when the element becomes visible in a window that is
        /// already open. (Before the window opens, the Opened handler covers it.)
        /// </summary>
        private static void Element_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.IsVisibleProperty && e.NewValue is true && sender is InputElement element) {
                TopLevel? topLevel = TopLevel.GetTopLevel(element);
                if (topLevel != null && topLevel.IsVisible)
                    FocusWhenReady(element);
            }
        }

        /// <summary>
        /// Focuses the element once its window can accept focus: on the window's Opened
        /// event if it has not been shown yet, otherwise after pending layout completes.
        /// </summary>
        /// <param name="element">The element to focus.</param>
        private static void FocusWhenReady(InputElement element)
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(element);
            if (topLevel == null)
                return;

            if (topLevel is Window window && !window.IsVisible) {
                // Focusing before the window is shown doesn't stick, so wait for Opened.
                EventHandler? handler = null;
                handler = (s, e) => {
                    window.Opened -= handler;
                    FocusElementOrDescendant(element);
                };
                window.Opened += handler;
            }
            else {
                Dispatcher.UIThread.Post(() => FocusElementOrDescendant(element), DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Focuses the element if it is focusable; otherwise focuses the first focusable
        /// element inside it (so the property can be placed on a UserControl or a page).
        /// Does nothing if the element is not visible (for example, a hidden wizard page).
        /// </summary>
        /// <param name="element">The element to focus.</param>
        private static void FocusElementOrDescendant(InputElement element)
        {
            if (!element.IsEffectivelyVisible)
                return;

            if (element.Focusable) {
                element.Focus();
            }
            else {
                IInputElement? descendant = FocusManager.FindFirstFocusableElement(element);
                descendant?.Focus();
            }
        }
    }
}
