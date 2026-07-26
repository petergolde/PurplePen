// ErrorDetailsDialog.axaml.cs
//
// Code-behind for the error details dialog. Everything the dialog shows is data-bound
// from ErrorDetailsDialogViewModel, so the only things here are copying the text to the
// clipboard and closing the window.

using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using AvPurplePen.ViewModels;
using System;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog showing the raw text of an unhandled exception. The caller must set
    /// DataContext to an <see cref="ViewModels.ErrorDetailsDialogViewModel"/> before showing.
    /// </summary>
    public partial class ErrorDetailsDialog : Window
    {
        /// <summary>
        /// Initializes the dialog and its components.
        /// </summary>
        public ErrorDetailsDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Copies the exception text to the system clipboard, so the user can paste it into
        /// an email or a forum post. Failures are ignored: this dialog is shown when the
        /// application is already in an unknown state, and a clipboard that is locked by
        /// another process must not produce a second error on top of the first.
        /// </summary>
        private async void CopyButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ErrorDetailsDialogViewModel vm)
                return;

            try {
                IClipboard? clipboard = GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(vm.ExceptionText);
            }
            catch (Exception) {
                // Clipboard access can fail if another application is holding it open.
            }
        }

        /// <summary>
        /// Closes the dialog.
        /// </summary>
        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }
    }
}
