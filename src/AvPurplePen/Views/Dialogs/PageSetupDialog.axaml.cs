// PageSetupDialog.axaml.cs
//
// Code-behind for the Page Setup dialog. All state is data-bound to
// PageSetupDialogViewModel; this file only handles the OK / Cancel buttons.
// The caller must set DataContext to a
// PurplePen.ViewModels.PageSetupDialogViewModel before showing, and read its
// PaperSizeWithMargins property back after the dialog returns true.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for choosing the page paper size, margins, and orientation. Hosts
    /// a PaperSizeControl in separate-margins mode.
    /// </summary>
    public partial class PageSetupDialog : Window
    {
        public PageSetupDialog()
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
