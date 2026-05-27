// AddTextLineDialog.axaml.cs
//
// Code-behind for the "Add Text Line" dialog. Everything visible is bound to the
// AddTextLineDialogViewModel; this file only handles the OK/Cancel buttons.
// The caller sets DataContext to an AddTextLineDialogViewModel (with ObjectName,
// EnableThisCourse, TextLine and TextLineKind populated) before showing, then
// reads TextLine and TextLineKind back after the dialog closes.
//
// Migrated from WinForms PurplePen/AddTextLine.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for entering a line of text to place on a separate line of the
    /// control descriptions. The DataContext must be an
    /// <see cref="PurplePen.ViewModels.AddTextLineDialogViewModel"/>.
    /// </summary>
    public partial class AddTextLineDialog : Window
    {
        /// <summary>
        /// Initializes the dialog and focuses the text entry when it opens.
        /// </summary>
        public AddTextLineDialog()
        {
            InitializeComponent();
            Opened += (s, e) => textBoxText.Focus();
        }

        /// <summary>
        /// Accepts the entered text and closes the dialog with a true result.
        /// </summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        /// <summary>
        /// Cancels and closes the dialog with a false result.
        /// </summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
