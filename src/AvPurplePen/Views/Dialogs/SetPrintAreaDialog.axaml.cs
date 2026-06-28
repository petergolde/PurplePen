// SetPrintAreaDialog.axaml.cs
//
// Code-behind for the interactive Set Print Area tool window. All print-area
// logic lives in SetPrintAreaDialogViewModel; this file only wires up the
// window lifecycle:
//   * Opened  → position near the owner and start the poll timer.
//   * Closing → stop the timer and let the ViewModel cancel the mode if needed.
//   * Done    → apply the area (the Controller then closes the window).
//   * Cancel  → close (the Closing handler cancels the mode).
//
// The 1/2-second timer replaces the WinForms updateTimer: it lets the ViewModel
// notice when the user has dragged the rectangle away from the automatic
// position and clear the "automatic" checkbox.
//
// Migrated from WinForms PurplePen/SetPrintAreaDialog.cs.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Owned (non-modal) dialog for setting a course's printing area. The caller
    /// sets the DataContext to a configured
    /// <see cref="SetPrintAreaDialogViewModel"/> and shows it via
    /// <c>IDialogService.ShowOwnedDialog(vm, disableOwner: false)</c> so the map
    /// stays interactive while the user drags the print rectangle.
    /// </summary>
    public partial class SetPrintAreaDialog : Window
    {
        // Polls the ViewModel so it can clear the "automatic" checkbox once the
        // user drags the rectangle away from the automatically-computed area.
        private readonly DispatcherTimer updateTimer;

        public SetPrintAreaDialog()
        {
            InitializeComponent();

            updateTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(500),
            };
            updateTimer.Tick += UpdateTimer_Tick;

            Opened += SetPrintAreaDialog_Opened;
            Closing += SetPrintAreaDialog_Closing;
        }

        /// <summary>Positions the dialog near the owner and starts the poll timer.</summary>
        private void SetPrintAreaDialog_Opened(object? sender, EventArgs e)
        {
            if (Owner is Window owner) {
                Position = new PixelPoint(owner.Position.X + 10, owner.Position.Y + 100);
            }

            updateTimer.Start();
        }

        /// <summary>Stops the timer and cancels the mode if closing without Done.</summary>
        private void SetPrintAreaDialog_Closing(object? sender, WindowClosingEventArgs e)
        {
            updateTimer.Stop();
            (DataContext as SetPrintAreaDialogViewModel)?.HandleClosing();
        }

        /// <summary>Periodically lets the ViewModel re-check the rectangle position.</summary>
        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            (DataContext as SetPrintAreaDialogViewModel)?.CheckRectangleMoved();
        }

        /// <summary>
        /// Applies the print area. The ViewModel ends the rectangle-select mode,
        /// which closes this window; the explicit Close is a harmless fallback.
        /// </summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            (DataContext as SetPrintAreaDialogViewModel)?.Confirm();
            Close();
        }

        /// <summary>Cancels; the Closing handler cancels the mode.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
