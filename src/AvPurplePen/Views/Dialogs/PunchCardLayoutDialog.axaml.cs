// PunchCardLayoutDialog.axaml.cs
//
// Code-behind for the Punch Card Layout dialog. All state is data-bound to
// PunchCardLayoutDialogViewModel; this file just handles OK/Cancel.
//
// Migrated from WinForms PurplePen/PunchcardLayoutDialog.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for configuring punch card layout (rows, columns, box order).
    /// The caller must set DataContext to a
    /// <see cref="PurplePen.ViewModels.PunchCardLayoutDialogViewModel"/> before
    /// showing, and read its <c>PunchcardFormat</c> property back after the
    /// dialog returns true.
    /// </summary>
    public partial class PunchCardLayoutDialog : Window
    {
        public PunchCardLayoutDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK.</summary>
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
