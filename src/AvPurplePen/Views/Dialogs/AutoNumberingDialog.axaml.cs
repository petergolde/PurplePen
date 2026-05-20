// AutoNumberingDialog.axaml.cs
//
// Code-behind for the Automatic Numbering dialog. All state is data-bound to
// AutoNumberingDialogViewModel; this file only handles the OK / Cancel buttons.
//
// Migrated from WinForms PurplePen/AutoNumbering.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for configuring automatic control numbering. The caller must set
    /// DataContext to a
    /// <see cref="PurplePen.ViewModels.AutoNumberingDialogViewModel"/> before
    /// showing, and read its properties back after the dialog returns true.
    /// </summary>
    public partial class AutoNumberingDialog : Window
    {
        public AutoNumberingDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK; the ViewModel holds the chosen settings.</summary>
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
