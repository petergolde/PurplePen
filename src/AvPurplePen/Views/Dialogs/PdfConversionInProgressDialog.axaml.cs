// PdfConversionInProgressDialog.axaml.cs
//
// Code-behind for the "Reading PDF" dialog. All state lives on the
// PdfConversionInProgressDialogViewModel; this file only handles the Cancel
// button. The dialog is shown while the PDFium converter reads a PDF map file;
// if the conversion fails, the caller calls the ViewModel's ShowErrorMessage to
// reveal the error details, after which the user dismisses the dialog with
// Cancel.
//
// Migrated from WinForms PurplePen/PdfConversionInProgress.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Modal dialog showing PDF conversion progress (and, on failure, the
    /// converter's error output). The caller must set DataContext to a
    /// <see cref="PurplePen.ViewModels.PdfConversionInProgressDialogViewModel"/>
    /// before showing.
    /// </summary>
    public partial class PdfConversionInProgressDialog : Window
    {
        public PdfConversionInProgressDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Cancel closes the dialog with false; the awaiting caller sees the
        /// false result and aborts the PDF conversion.
        /// </summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
