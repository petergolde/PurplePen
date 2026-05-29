// SelectLocationsForMoveDialog.axaml.cs
//
// Code-behind for the interactive Move All Controls location-selection dialog.
// The staging logic lives in SelectLocationsForMoveDialogViewModel; this file
// only wires up the window lifecycle to the ViewModel:
//   * Opened  → position near the owner and start the first step.
//   * Closing → let the ViewModel finish/undo the move if not confirmed.
//   * Confirm → commit the move, then close.
//   * Cancel  → close (Closing handles the undo).
//
// Migrated from WinForms PurplePen/SelectLocationsForMove.cs.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Owned (non-modal) dialog that guides the user through selecting controls
    /// and new locations for the Move All Controls command. The caller sets the
    /// DataContext to a configured
    /// <see cref="SelectLocationsForMoveDialogViewModel"/> and shows it via
    /// <c>IDialogService.ShowOwnedDialog(vm, disableOwner: false)</c> so the map
    /// stays interactive.
    /// </summary>
    public partial class SelectLocationsForMoveDialog : Window
    {
        public SelectLocationsForMoveDialog()
        {
            InitializeComponent();
            Opened += SelectLocationsForMoveDialog_Opened;
            Closing += SelectLocationsForMoveDialog_Closing;
        }

        /// <summary>Positions the dialog near the owner and starts the first step.</summary>
        private void SelectLocationsForMoveDialog_Opened(object? sender, System.EventArgs e)
        {
            if (Owner is Window owner) {
                Position = new PixelPoint(owner.Position.X + 10, owner.Position.Y + 130);
            }

            (DataContext as SelectLocationsForMoveDialogViewModel)?.Start();
        }

        /// <summary>Finishes/undoes the move if the dialog is closing unconfirmed.</summary>
        private void SelectLocationsForMoveDialog_Closing(object? sender, WindowClosingEventArgs e)
        {
            (DataContext as SelectLocationsForMoveDialogViewModel)?.HandleClosing();
        }

        /// <summary>Commits the move, then closes the dialog.</summary>
        private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
        {
            (DataContext as SelectLocationsForMoveDialogViewModel)?.Confirm();
            Close();
        }

        /// <summary>Cancels; the Closing handler undoes the live preview.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
