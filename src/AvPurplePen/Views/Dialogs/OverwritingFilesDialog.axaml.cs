// OverwritingFilesDialog.axaml.cs
//
// Code-behind for the "Confirm Replace Files" dialog. Everything is bound to
// the OverwritingFilesDialogViewModel; this file just handles the OK / Cancel
// buttons.
//
// Migrated from WinForms PurplePen/OverwritingOcadFilesDialog.cs (renamed).

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// "Confirm Replace Files" dialog. The caller must set DataContext to an
    /// <see cref="PurplePen.ViewModels.OverwritingFilesDialogViewModel"/>
    /// (with Filenames populated) before showing.
    /// </summary>
    public partial class OverwritingFilesDialog : Window
    {
        public OverwritingFilesDialog()
        {
            InitializeComponent();
        }

        /// <summary>Confirms the overwrite and closes with OK.</summary>
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
