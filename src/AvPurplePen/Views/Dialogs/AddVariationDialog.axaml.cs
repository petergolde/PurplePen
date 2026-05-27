// AddVariationDialog.axaml.cs
//
// Code-behind for the Add Variation dialog. All state is data-bound to
// AddVariationDialogViewModel; this file only handles the OK / Cancel buttons.
//
// Migrated from WinForms PurplePen/AddForkDialog.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for adding a fork or loop variation. The caller must set
    /// DataContext to a
    /// <see cref="PurplePen.ViewModels.AddVariationDialogViewModel"/> before
    /// showing, and read IsLoop / NumberOfBranches back after it returns true.
    /// </summary>
    public partial class AddVariationDialog : Window
    {
        public AddVariationDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK; the ViewModel holds the chosen variation.</summary>
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
