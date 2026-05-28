// MoveAllControlsDialog.axaml.cs
//
// Code-behind for the Move All Controls dialog. All state is data-bound to
// MoveAllControlsDialogViewModel; this file only handles the OK / Cancel
// buttons.
//
// Migrated from WinForms PurplePen/MoveAllControls.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for choosing how to move all controls on the map. The caller must
    /// set DataContext to a
    /// <see cref="PurplePen.ViewModels.MoveAllControlsDialogViewModel"/> before
    /// showing, and read its Action back after the dialog returns true.
    /// </summary>
    public partial class MoveAllControlsDialog : Window
    {
        public MoveAllControlsDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK; the ViewModel holds the chosen action.</summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
