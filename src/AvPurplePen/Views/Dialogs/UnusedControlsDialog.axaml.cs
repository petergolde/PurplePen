// UnusedControlsDialog.axaml.cs
//
// Code-behind for the Unused Controls dialog. All state is data-bound
// to the UnusedControlsDialogViewModel. This file only handles
// OK / Cancel buttons.
//
// Migrated from WinForms PurplePen/UnusedControls.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog listing unused control points for deletion. The caller must set
    /// DataContext to an <see cref="PurplePen.ViewModels.UnusedControlsDialogViewModel"/>
    /// (with ControlItems populated via SetControlsToDelete) before showing.
    /// </summary>
    public partial class UnusedControlsDialog : Window
    {
        public UnusedControlsDialog()
        {
            InitializeComponent();
        }

        /// <summary>Closes with OK; the ViewModel holds the checked state.</summary>
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
